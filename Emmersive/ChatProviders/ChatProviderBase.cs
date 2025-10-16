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
    private DateTime _cooldownUntil = DateTime.MinValue;
    private UIInputText? _modelInput;

    protected string? UnavailableReason;

    protected ChatProviderBase(string apiKey)
    {
        ServiceCount++;
        ApiKey = apiKey;
    }

    public static int ServiceCount { get; internal set; }

    [JsonIgnore]
    public abstract PromptExecutionSettings ExecutionSettings { get; set; }

    protected string ApiKey { get; set; }

    [JsonProperty]
    protected string EncryptedKey
    {
        get => ApiKey.EncryptAes();
        set => ApiKey = value.DecryptAes();
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
        UnavailableReason = message;
        _cooldownUntil = DateTime.Now + TimeSpan.FromSeconds(EmConfig.Policy.ServiceCooldown.Value);
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
        var activity = EmActivity.Current!;
        activity.SetStatus(EmActivity.StatusType.InProgress);

        var timeout = EmConfig.Policy.Timeout.Value;

        var service = kernel.GetRequiredService<IChatCompletionService>(Id);
        var task = service.GetChatMessageContentAsync(context, ExecutionSettings, kernel, token)
            .AsUniTask(false)
            .Preserve();

        var tasklet = await UniTask.WhenAny(task, UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: token));
        if (!tasklet.hasResultLeft) {
            activity.SetStatus(EmActivity.StatusType.Timeout);
        }

        var response = await task;

        HandleRequestInternal(response, activity);

        return response;
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

    protected abstract void HandleRequestInternal(ChatMessageContent response, EmActivity activity);
}