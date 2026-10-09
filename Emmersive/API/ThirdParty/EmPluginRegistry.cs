using System;
using System.Collections.Generic;
using System.Linq;

namespace Emmersive.API.ThirdParty;

public sealed class EmPluginRegistry
{
    private readonly List<IContextProvider> _externalContextProviders = [];
    private readonly List<ILayoutProvider> _externalLayoutProviders = [];

    public IReadOnlyList<IContextProvider> ExternalContextProviders => _externalContextProviders;
    public IReadOnlyList<ILayoutProvider> ExternalLayoutProviders => _externalLayoutProviders;

    public static EmPluginRegistry Instance => field ??= new();

    public void RegisterContextProvider(IContextProvider provider)
    {
        if (provider is null) {
            throw new ArgumentNullException(nameof(provider));
        }

        if (_externalContextProviders.Any(p => ReferenceEquals(p, provider))) {
            EmMod.Warn<EmPluginRegistry>($"context provider {provider.Name} is already registered");
            return;
        }

        _externalContextProviders.Add(provider);
        EmMod.Log<EmPluginRegistry>($"registered external context provider: {provider.Name}");
    }

    public void UnregisterContextProvider(IContextProvider provider)
    {
        _externalContextProviders.RemoveAll(p => ReferenceEquals(p, provider));
    }

    public void RegisterLayoutProvider(ILayoutProvider provider)
    {
        if (provider is null) {
            throw new ArgumentNullException(nameof(provider));
        }

        if (_externalLayoutProviders.Any(p => ReferenceEquals(p, provider))) {
            EmMod.Warn<EmPluginRegistry>($"layout provider {provider.GetType().Name} is already registered");
            return;
        }

        _externalLayoutProviders.Add(provider);
        EmMod.Log<EmPluginRegistry>($"registered external layout provider: {provider.GetType().Name}");
    }

    public void UnregisterLayoutProvider(ILayoutProvider provider)
    {
        _externalLayoutProviders.RemoveAll(p => ReferenceEquals(p, provider));
    }
}