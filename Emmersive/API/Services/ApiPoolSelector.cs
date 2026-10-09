using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Emmersive.ChatProviders;
using Emmersive.Helper;
using Emmersive.LangMod;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Services;

namespace Emmersive.API.Services;

public sealed class ApiPoolSelector : IAIServiceSelector
{
    private readonly List<IChatProvider> _providers = [];

    public IReadOnlyList<IChatProvider> Providers => _providers;

    public IChatProvider? CurrentProvider { get; private set; }

    public static ApiPoolSelector Instance => field ??= new();

    internal string? LoadFailedBackupDir { get; private set; }

    public void AddService(IChatProvider provider)
    {
        provider.RemoveProviderParam();
        AddServiceInternal(provider);
        LoadFailedBackupDir = null;

        SaveServices();
    }

    public void ReorderService(IChatProvider provider, int mod)
    {
        _providers.Move(provider, mod);

        SaveServices();
    }

    public void RemoveService(IChatProvider provider)
    {
        _providers.Remove(provider);

        if (CurrentProvider == provider) {
            CurrentProvider = null;
        }

        provider.RemoveProviderParam();

        EmMod.Log<ApiPoolSelector>($"removed {provider.Id}");

        SaveServices();
    }

    public void ClearServices()
    {
        foreach (var provider in _providers.ToArray()) {
            RemoveService(provider);
        }
    }

    public void SaveServices()
    {
        if (LoadFailedBackupDir is not null && _providers.Count == 0) {
            return;
        }

        var context = ResourceFetch.Context;

        context.Save("active_providers", _providers);

        foreach (var provider in _providers) {
            provider.SaveProviderParam();
        }

        context.SaveUncompressed("service_count", ChatProviderBase.ServiceCount);
    }

    public void LoadServices(bool clear = true)
    {
        EmMod.Log<ApiPoolSelector>("loading active services");

        if (clear) {
            _providers.Clear();
        }

        var context = ResourceFetch.Context;

        List<IChatProvider>? providers = null;
        try {
            context.Load("active_providers", out providers);
        } catch (Exception ex) {
            BackupChunk(context, "active_providers");
            LoadFailedBackupDir = Path.Combine(context.ChunkDir.FullName, "backup");
            EmMod.ErrorWithPopup<ApiPoolSelector>("em_ui_err_services_load".Loc(LoadFailedBackupDir), ex);
        }

        foreach (var provider in providers ?? []) {
            AddServiceInternal(provider);
        }

        var undecrypted = _providers
            .OfType<ChatProviderBase>()
            .Where(p => p.KeyDecryptFailed)
            .Select(p => p.Alias)
            .ToArray();
        if (undecrypted.Length > 0) {
            BackupChunk(context, "active_providers");
            EmMod.Popup<ApiPoolSelector>($"[{string.Join(", ", undecrypted)}] {"em_ui_err_key_decrypt".lang()}", 10f);
        }

        if (context.Load<int>("service_count", out var serviceCount)) {
            ChatProviderBase.ServiceCount = serviceCount;
        }
    }

    private static void BackupChunk(GameIOContext context, string chunkName)
    {
        try {
            var dir = context.ChunkDir.CreateSubdirectory("backup");
            foreach (var file in context.ChunkDir.GetFiles($"{chunkName}.*")) {
                file.CopyTo(Path.Combine(dir.FullName, $"{chunkName}_{DateTime.Now:yyyyMMdd_HHmmss}{file.Extension}"), true);
            }
        } catch {
            // noexcept
        }
    }

    private void AddServiceInternal(IChatProvider provider)
    {
        _providers.Add(provider);

        provider.LoadProviderParam();

        EmMod.Log<ApiPoolSelector>($"added {provider.Id}");
    }

#region Test Services

    internal static void MockTestServices()
    {
        var apiPool = Instance;
        var keyFile = PackageIterator
            .GetMapping(ModInfo.Guid)
            .RelocateFile("Emmersive/DebugKeys.json");

        var keys = IO.LoadFile<Dictionary<string, string[]>>(keyFile.FullName);

        if (keys is null) {
            return;
        }

        foreach (var key in keys["Em_GoogleGeminiAPI_Dummy"]) {
            apiPool.AddService(new GoogleProvider(key) {
                CurrentModel = "gemini-3-flash",
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

    public bool HasAnyAvailableServices()
    {
        return CurrentProvider?.IsAvailable is true ||
               (_providers.Count > 0 && _providers.Any(p => p.IsAvailable));
    }

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