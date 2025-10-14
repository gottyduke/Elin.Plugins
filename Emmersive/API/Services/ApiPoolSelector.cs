using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Services;

namespace Emmersive.API.Services;

public sealed class ApiPoolSelector : IAIServiceSelector
{
    private readonly List<IChatProvider> _providers = [];

    public IReadOnlyList<IChatProvider> Providers => _providers;
    public IChatProvider? CurrentProvider { get; private set; }

    [field: AllowNull]
    public static ApiPoolSelector Instance => field ??= new();

    public void AddService(IChatProvider provider)
    {
        _providers.Add(provider);
        EmMod.Log<ApiPoolSelector>($"added {provider.Id}");
    }

    public void RemoveService(IChatProvider provider)
    {
        if (_providers.Remove(provider)) {
            EmMod.Log<ApiPoolSelector>($"removed {provider.Id}");
        }
    }

    public void ClearServices()
    {
        _providers.Clear();
        EmMod.Log<ApiPoolSelector>("cleared services");
    }

#region AI Selector

    public bool TrySelectAIService<T>(Kernel kernel,
                                      KernelFunction function,
                                      KernelArguments arguments,
                                      [NotNullWhen(true)] out T? service,
                                      out PromptExecutionSettings? serviceSettings) where T : class, IAIService
    {
        if (TryGetNextAvailable(out var provider)) {
            service = kernel.GetRequiredService<T>(provider.Id);
            serviceSettings = provider.ExecutionSettings;
            return true;
        }

        service = null;
        serviceSettings = null;
        return false;
    }

    public bool TryGetNextAvailable([NotNullWhen(true)] out IChatProvider? next)
    {
        foreach (var provider in _providers) {
            provider.UpdateAvailability();

            if (!provider.IsAvailable) {
                continue;
            }

            EmMod.Debug<ApiPoolSelector>($"using {provider.Id}");

            next = CurrentProvider = provider;
            return true;
        }

        EmMod.Warn<ApiPoolSelector>($"no chat provider available, {_providers.Count} registered");

        next = null;
        return false;
    }

#endregion
}