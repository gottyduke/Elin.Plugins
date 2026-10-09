using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.API;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public abstract class ContextProviderBase : IContextProvider
{
    public bool IsAvailable => Name == "system_prompt" || !IsDisabled(Name);

    public abstract string Name { get; }

    public virtual object? Build()
    {
        var data = BuildInternal();
        if (data is null) {
            return null;
        }

        if (EmConfig.Context.EnableLocalizer.Value) {
            Localize(data);
        }

        return data;
    }

    internal static bool IsDisabled(string name)
    {
        var disabled = EmConfig.Context.DisabledProviders.Value;
        if (disabled.IsEmptyOrNull) {
            return false;
        }

        return disabled
            .Split(',')
            .Any(entry => string.Equals(entry.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }

    protected virtual void Localize(IDictionary<string, object> data, string? prefixOverride = null)
    {
        var prefix = prefixOverride ?? Name;

        foreach (var (k, v) in data.ToArray()) {
            if (!$"{prefix}_{k}".TryLocalize(out var i18N)) {
                continue;
            }

            data.Remove(k);
            data[i18N] = v;
        }
    }

    protected virtual IDictionary<string, object>? BuildInternal()
    {
        return null;
    }
}