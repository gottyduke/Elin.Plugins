using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
public abstract partial class ChatProviderBase : IChatProvider, IExtensionRequestMerger
{
    private DateTime _cooldownUntil = DateTime.MinValue;
    private float _timeoutIncremental;

    protected string? UnavailableReason;

    protected ChatProviderBase(string apiKey)
    {
        ServiceCount++;
        ApiKey = apiKey;
    }

    [JsonProperty]
    public abstract string EndPoint { get; set; }

    [JsonProperty]
    public abstract string Alias { get; set; }

    public static int ServiceCount { get; internal set; }

    public abstract PromptExecutionSettings ExecutionSettings { get; set; }

    protected string ApiKey { get; set; }

    [JsonProperty]
    protected string EncryptedKey
    {
        get => ApiKey.EncryptAes();
        set => ApiKey = value.DecryptAes();
    }

    [JsonProperty]
    public virtual string Id
    {
        get => field ??= $"{Alias}#{ServiceCount}";
        set;
    }

    [JsonProperty]
    public abstract string CurrentModel { get; set; }

    public abstract IDictionary<string, object> RequestParams { get; set; }

    public virtual bool IsAvailable => DateTime.UtcNow >= _cooldownUntil;

    public void Register(IKernelBuilder builder)
    {
        Register(builder, CurrentModel);
    }

    public virtual void MarkUnavailable(string? message = null)
    {
        UnavailableReason = message;
        _cooldownUntil = DateTime.UtcNow + TimeSpan.FromSeconds(EmConfig.Policy.ServiceCooldown.Value + _timeoutIncremental);
        _timeoutIncremental += 1f;
        EmMod.DebugPopup<IChatProvider>($"[{Id}] temporarily unavailable: {message}");
    }

    public virtual void UpdateAvailability()
    {
        if (DateTime.UtcNow >= _cooldownUntil) {
            _cooldownUntil = DateTime.MinValue;
        }
    }

    public virtual async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var activity = EmActivity.FromProviderLatest(Id);
        activity?.SetStatus(EmActivity.StatusType.InProgress);

        HandleRequestInternal();

        var timeout = EmConfig.Policy.Timeout.Value;

        var service = kernel.GetRequiredService<IChatCompletionService>(Id);
        var task = service.GetChatMessageContentAsync(context, ExecutionSettings, kernel, token)
            .AsUniTask(false)
            .Preserve();

        var tasklet = await UniTask.WhenAny(task, UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: token));
        if (!tasklet.hasResultLeft) {
            activity?.SetStatus(EmActivity.StatusType.Timeout);
        }

        var response = await task;

        if (activity is not null) {
            HandleRequestActivity(response, activity);
        }

        _timeoutIncremental = 0f;

        return response;
    }

    public virtual void MergeExtensionRequest(IDictionary<string, object> data, HttpRequestMessage request)
    {
        if (RequestParams.Count == 0) {
            return;
        }

        foreach (var (k, v) in RequestParams) {
            if (!k.IsEmptyOrNull && v is not null) {
                data[k] = v;
            }
        }
    }

    protected abstract void Register(IKernelBuilder builder, string model);

    protected abstract void HandleRequestActivity(ChatMessageContent response, EmActivity activity);

    protected abstract void HandleRequestInternal();
}