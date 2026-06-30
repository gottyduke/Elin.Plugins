using System.Collections.Generic;

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
        _externalContextProviders.Add(provider);
        EmMod.Log<EmPluginRegistry>($"registered external context provider: {provider.Name}");
    }

    public void RegisterLayoutProvider(ILayoutProvider provider)
    {
        _externalLayoutProviders.Add(provider);
        EmMod.Log<EmPluginRegistry>("registered external layout provider");
    }
}