using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cwl.Helper.FileUtil;
using Emmersive.ChatProviders;
using Emmersive.Helper;
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

        provider.LoadProviderParam();

        EmMod.Log<ApiPoolSelector>($"added {provider.Id}");
    }

    public void ReorderService(IChatProvider provider, int mod)
    {
        _providers.Move(provider, mod);
    }

    public void RemoveService(IChatProvider provider)
    {
        _providers.Remove(provider);

        if (CurrentProvider == provider) {
            CurrentProvider = null;
        }

        provider.RemoveProviderParam();

        EmMod.Log<ApiPoolSelector>($"removed {provider.Id}");
    }

    public void ClearServices()
    {
        foreach (var provider in _providers) {
            RemoveService(provider);
        }
    }

    public void SaveServices()
    {
        var context = ResourceFetch.Context;

        context.Save(_providers, "active_providers");

        foreach (var provider in _providers) {
            provider.SaveProviderParam();
        }

        context.SaveUncompressed(ChatProviderBase.ServiceCount, "service_count");
    }

    public void LoadServices(bool clear = true)
    {
        EmMod.Log<ApiPoolSelector>("loading active services");

        if (clear) {
            _providers.Clear();
        }

        var context = ResourceFetch.Context;

        if (!context.Load<List<IChatProvider>>(out var providers, "active_providers")) {
            return;
        }

        foreach (var provider in providers) {
            AddService(provider);
            provider.LoadProviderParam();
        }

        if (context.Load<int>(out var serviceCount, "service_count")) {
            ChatProviderBase.ServiceCount = serviceCount;
        }
    }

    public void CleanServiceParams()
    {
        var container = Path.Combine(ResourceFetch.CustomFolder, "params");
        if (!Directory.Exists(container)) {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(container, "*", SearchOption.TopDirectoryOnly)) {
            var name = Path.GetFileNameWithoutExtension(file);
            if (_providers.All(p => p.Id != name)) {
                File.Delete(file);
            }
        }
    }

#region Test Services

    internal static void MockTestServices()
    {
        var apiPool = Instance;
        var (_, keys) = PackageIterator
            .GetJsonFromPackage<Dictionary<string, string[]>>("Emmersive/DebugKeys.json", ModInfo.Guid);

        if (keys is null) {
            return;
        }

        foreach (var key in keys["Em_GoogleGeminiAPI_Dummy"]) {
            apiPool.AddService(new GoogleProvider(key) {
                CurrentModel = "gemini-2.5-flash",
            });
        }

        foreach (var key in keys["Em_DeepSeekAPI_Dummy"]) {
            apiPool.AddService(new OpenAIProvider(key) {
                EndPoint = "https://api.deepseek.com/v1",
                Alias = "DeepSeek",
                CurrentModel = "deepseek-chat",
            });
        }

        foreach (var key in keys["Em_OpenAIAPI_Dummy"]) {
            apiPool.AddService(new OpenAIProvider(key) {
                CurrentModel = "gpt-5-nano",
            });
        }
    }

#endregion

#region AI Selector

    public bool TrySelectAIService<T>(Kernel kernel,
                                      KernelFunction function,
                                      KernelArguments arguments,
                                      [NotNullWhen(true)] out T? service,
                                      out PromptExecutionSettings? serviceSettings) where T : class, IAIService
    {
        serviceSettings = null;

        if (TryGetNextAvailable(out var provider)) {
            service = kernel.GetRequiredService<T>(provider.Id);
            return true;
        }

        service = null;
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

        next = CurrentProvider = null;
        return false;
    }

#endregion
}