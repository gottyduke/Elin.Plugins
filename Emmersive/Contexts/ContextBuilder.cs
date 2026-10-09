using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Emmersive.API;
using Emmersive.API.ThirdParty;
using Emmersive.Helper;
using EModding.Helper.Runtime.Exceptions;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;

namespace Emmersive.Contexts;

public sealed class ContextBuilder
{
    private readonly List<IContextProvider> _providers = [];

    internal IReadOnlyList<Chara> TriggerCharas { get; set; } = [];
    internal IReadOnlyList<Chara> NearbyCharas { get; set; } = [];
    internal IReadOnlyCollection<string>? ShownLines { get; set; }
    internal bool IsManual { get; set; }

    private ContextBuilder()
    {
    }


    public static IContextProvider RecentActionContext => field ??= new RecentActionContext();

    public static IContextProvider CurrentZoneContext => new ZoneContext(EClass._zone);
    public static IContextProvider PlayerContext => new PlayerContext();


    public static IContextProvider SystemContext
    {
        get => field ??= new SystemContext();
        set;
    }

    /// <summary>
    ///     <see cref="CurrentZoneContext" />
    ///     <see cref="PlayerContext" />
    /// </summary>
    public static ContextBuilder CreateStandardPrefix()
    {
        return new ContextBuilder()
            .Add(CurrentZoneContext)
            .Add(PlayerContext);
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

    public ContextBuilder AddExternalProviders()
    {
        _providers.AddRange(EmPluginRegistry.Instance.ExternalContextProviders
            .Where(p => p.IsAvailable && !ContextProviderBase.IsDisabled(p.Name)));
        return this;
    }

    public KernelArguments? Build()
    {
        if (!EClass.core.IsGameStarted) {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var verbose = EmConfig.Policy.Verbose.Value;

        var sb = new StringBuilder();

        foreach (var provider in _providers.Where(provider => provider.IsAvailable)) {
            try {
                var current = sw.Elapsed;

                var context = provider.Build();
                if (context is null) {
                    continue;
                }

                sb.AppendLine($"[{provider.Name}]");
                sb.AppendLine(Serialize(provider, context, false));

                if (verbose) {
                    EmMod.Debug<ContextBuilder>(
                        $"{provider.Name} {(sw.Elapsed - current).TotalMilliseconds:F1}ms\n{Serialize(provider, context, true)}");
                }
            } catch (Exception ex) {
                EmMod.Warn<ContextBuilder>($"provider {provider.Name} failed\n{ex}");
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        var language = MOD.langs.TryGetValue(Lang.langCode, out var lang)
            ? $"{lang.name}({lang.name_en})"
            : Lang.langCode;

        var data = new KernelArguments {
            ["system_prompt"] = SystemContext.Build(),
            ["game_contexts"] = $"Current game state in JSON:\n{sb}",
            ["language_code"] = language,
            ["max_reactions"] = EmConfig.Scene.MaxReactions.Value,
        };

        sw.Stop();
        EmMod.Debug<ContextBuilder>($"took {sw.Elapsed.TotalMilliseconds:F1}ms");

        return data;
    }

    private static string Serialize(IContextProvider provider, object context, bool indented)
    {
        if (provider is ContextProviderBase) {
            return indented ? context.ToIndentedJson() : context.ToCompactJson();
        }

        return JsonConvert.SerializeObject(context, indented ? Formatting.Indented : Formatting.None);
    }

    public static void ResetAllContexts()
    {
        SystemContext = null!;
    }
}