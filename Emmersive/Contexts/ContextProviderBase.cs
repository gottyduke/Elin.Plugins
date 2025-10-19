using System.Collections.Generic;
using System.Linq;
using Emmersive.API;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public abstract class ContextProviderBase : IContextProvider
{
    public bool IsAvailable => !EmConfig.Context.DisabledProviders.Value.Contains(Name);
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