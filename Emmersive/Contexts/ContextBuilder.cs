using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Emmersive.API;
using Emmersive.Helper;
using Microsoft.SemanticKernel;

namespace Emmersive.Contexts;

public sealed class ContextBuilder
{
    private readonly List<IContextProvider> _providers = [];

    private ContextBuilder()
    {
    }

    [field: AllowNull]
    public static IContextProvider EnvironmentContext => field ??= new EnvironmentContext();

    [field: AllowNull]
    public static IContextProvider RecentActionContext => field ??= new RecentActionContext();

    public static IContextProvider CurrentZoneContext => new ZoneContext(EClass._zone);
    public static IContextProvider PlayerContext => new PlayerContext();

    [field: AllowNull]
    public static IContextProvider SystemContext
    {
        get => field ??= new SystemContext();
        set;
    }

    public static ContextBuilder CreateStandard()
    {
        return new ContextBuilder()
            .Add(EnvironmentContext)
            .Add(CurrentZoneContext)
            .Add(PlayerContext)
            .Add(RecentActionContext);
    }

    public static ContextBuilder CreateDefault()
    {
        return new();
    }

    public ContextBuilder Add(IContextProvider provider)
    {
        _providers.Add(provider);
        return this;
    }

    public ContextBuilder Add(params IContextProvider[] providers)
    {
        _providers.AddRange(providers);
        return this;
    }

    public KernelArguments Build()
    {
        if (!EClass.core.IsGameStarted) {
            return [];
        }

        var sw = Stopwatch.StartNew();

        using var sb = StringBuilderPool.Get();

        foreach (var provider in _providers.Where(provider => provider.IsAvailable)) {
            try {
                var context = provider.Build();
                if (context is null) {
                    continue;
                }

                sb.AppendLine($"[{provider.Name}]");
                sb.AppendLine(context.ToCompactJson());

                EmMod.Debug<ContextBuilder>($"{provider.Name}\n{context.ToIndentedJson()}");
            } catch (Exception ex) {
                EmMod.Warn<ContextBuilder>($"provider {provider.Name} failed\n{ex}");
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        var lang = MOD.langs[Lang.langCode];
        var data = new KernelArguments {
            ["system_prompt"] = SystemContext.Build(),
            ["game_contexts"] = $"Current game state in JSON:\n{sb}",
            ["language_code"] = $"{lang.name}({lang.name_en})",
            ["max_reactions"] = EmConfig.Scene.MaxReactions.Value,
        };

        sw.Stop();
        EmMod.Debug<ContextBuilder>($"took {sw.Elapsed.Milliseconds}ms");

        return data;
    }

    public static void ResetAllContexts()
    {
        SystemContext = null!;
    }
}