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

    public void Register(IKernelBuilder builder, IChatProvider provider)
    {
        _providers.Add(provider);
        provider.Register(builder);
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
}