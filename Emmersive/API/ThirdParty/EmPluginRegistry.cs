using System;
using System.Collections.Generic;
using System.Linq;
using YKF;

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
            EmMod.Warn<EmPluginRegistry>($"layout provider {GetProviderName(provider)} is already registered");
            return;
        }

        _externalLayoutProviders.Add(provider);
        EmMod.Log<EmPluginRegistry>($"registered external layout provider: {GetProviderName(provider)}");
    }

    public ILayoutProvider RegisterLayoutProvider(Action<YKLayout> onLayout, Action? onConfirm = null)
    {
        if (onLayout is null) {
            throw new ArgumentNullException(nameof(onLayout));
        }

        var existing = _externalLayoutProviders.OfType<DelegateLayoutProvider>()
            .FirstOrDefault(p => p.Wraps(onLayout, onConfirm));
        if (existing is not null) {
            EmMod.Warn<EmPluginRegistry>($"layout provider {GetProviderName(existing)} is already registered");
            return existing;
        }

        var provider = new DelegateLayoutProvider(onLayout, onConfirm);
        RegisterLayoutProvider(provider);
        return provider;
    }

    public static string GetProviderName(ILayoutProvider provider)
    {
        return provider is DelegateLayoutProvider wrapped ? wrapped.Name : provider.GetType().Name;
    }

    public void UnregisterLayoutProvider(ILayoutProvider provider)
    {
        _externalLayoutProviders.RemoveAll(p => ReferenceEquals(p, provider));
    }

    private sealed class DelegateLayoutProvider(Action<YKLayout> onLayout, Action? onConfirm) : ILayoutProvider
    {
        public string Name => onLayout.Method.DeclaringType?.FullName ?? onLayout.Method.Name;

        public void OnLayout(YKLayout layout)
        {
            onLayout(layout);
        }

        public void OnLayoutConfirm()
        {
            onConfirm?.Invoke();
        }

        public bool Wraps(Action<YKLayout> layout, Action? confirm)
        {
            return onLayout.Equals(layout) && Equals(onConfirm, confirm);
        }
    }
}