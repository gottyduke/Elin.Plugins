using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;

namespace Emmersive.ChatProviders;

public abstract partial class ChatProviderBase : IChatProvider, IExtensionMerger
{
    [JsonIgnore]
    private DateTime _cooldownUntil = DateTime.MinValue;

    [JsonIgnore]
    private UIInputText? _modelInput;

    protected ChatProviderBase()
    {
        ServiceCount++;
    }

    public static int ServiceCount { get; internal set; }

    [JsonIgnore]
    public abstract PromptExecutionSettings ExecutionSettings { get; set; }

    [JsonIgnore]
    public required string ApiKey { protected get; set; }

    [JsonProperty]
    protected string EncryptedKey
    {
        get => ApiKey.DecryptAes();
        set => ApiKey = value.EncryptAes();
    }

    public abstract string Id { get; set; }
    public abstract string CurrentModel { get; set; }

    [JsonIgnore]
    public abstract IDictionary<string, object> RequestParams { get; set; }

    public virtual bool IsAvailable => DateTime.Now >= _cooldownUntil;

    public void Register(IKernelBuilder builder)
    {
        Register(builder, CurrentModel);
    }

    public virtual void MarkUnavailable(string? message = null)
    {
        _cooldownUntil = DateTime.Now + TimeSpan.FromSeconds(15f);
        EmMod.Warn($"[{Id}] temporarily unavailable: {message}");
        EmMod.DebugPopup<IChatProvider>($"[{Id}] temporarily unavailable: {message}");
    }

    public virtual void UpdateAvailability()
    {
        if (DateTime.Now >= _cooldownUntil) {
            _cooldownUntil = DateTime.MinValue;
        }
    }

    public virtual async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var service = kernel.GetRequiredService<IChatCompletionService>(Id);
        return await service.GetChatMessageContentAsync(context, ExecutionSettings, kernel, token)
            .ConfigureAwait(false);
    }

    public virtual void MergeExtensionData(IDictionary<string, object> data)
    {
        if (RequestParams.Count == 0) {
            return;
        }

        foreach (var (k, v) in RequestParams) {
            if (!k.IsEmpty() && v is not null) {
                data[k] = v;
            }
        }
    }

    protected abstract void Register(IKernelBuilder builder, string model);
}