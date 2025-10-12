using System.Collections.Generic;
using System.Linq;
using Emmersive.API;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public abstract class ContextProviderBase : IContextProvider
{
    public bool IsDisabled => EmConfig.Context.DisabledProviders.Value.Contains(Name);
    public abstract string Name { get; }

    public virtual object? Build()
    {
        var data = BuildCore();
        if (data is null) {
            return null;
        }

        Localize(data);

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

    protected virtual IDictionary<string, object>? BuildCore()
    {
        return null;
    }
}