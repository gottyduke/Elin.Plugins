using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
public abstract partial class ChatProviderBase : IChatProvider, IExtensionRequestMerger
{
    private static readonly AsyncLocal<(IChatProvider? Provider, bool Raw, JObject? Schema)> _scope = new();

    private DateTime _cooldownUntil = DateTime.MinValue;
    private DateTime _lastPopup = DateTime.MinValue;
    private float _timeoutIncremental;
    private string? _undecryptedKey;

    protected ChatProviderBase(string apiKey)
    {
        ServiceCount++;
        ApiKey = apiKey;
    }

    internal static (IChatProvider? Provider, bool Raw, JObject? Schema) CurrentScope => _scope.Value;

    [JsonProperty]
    public abstract string EndPoint { get; set; }

    [JsonProperty]
    public abstract string Alias { get; set; }

    public static int ServiceCount { get; internal set; }

    public abstract PromptExecutionSettings ExecutionSettings { get; set; }

    protected virtual PromptExecutionSettings RawExecutionSettings => ExecutionSettings;

    protected virtual bool RequiresApiKey => true;

    protected virtual bool IsConfigured => true;

    protected virtual float RequestTimeout => EmConfig.Policy.Timeout.Value;

    internal float TimeoutSeconds => RequestTimeout;

    protected string ApiKey { get; set; }

    internal bool KeyDecryptFailed => RequiresApiKey && _undecryptedKey is not null;

    protected string? UnavailableReason
    {
        get => field ?? (KeyDecryptFailed ? "em_ui_err_key_decrypt".lang() : null);
        set;
    }

    [JsonProperty]
    protected string EncryptedKey
    {
        get => _undecryptedKey ?? ApiKey.EncryptAes();
        set {
            try {
                ApiKey = value.DecryptAes();
                _undecryptedKey = null;
            } catch (Exception ex) {
                ApiKey = "";
                _undecryptedKey = value;
                EmMod.Warn<IChatProvider>($"[{Alias}] failed to decrypt api key: {ex.Message}");
            }
        }
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

    public virtual bool IsAvailable =>
        DateTime.UtcNow >= _cooldownUntil &&
        IsConfigured &&
        (!RequiresApiKey || !ApiKey.IsEmptyOrNull);

    public void Register(IKernelBuilder builder)
    {
        Register(builder, CurrentModel);
    }

    public virtual void MarkUnavailable(string? message = null)
    {
        UnavailableReason = message;
        _cooldownUntil = DateTime.UtcNow + TimeSpan.FromSeconds(EmConfig.Policy.ServiceCooldown.Value + _timeoutIncremental);
        _timeoutIncremental += 1f;

        if (!message.IsEmptyOrNull && !_testing && DateTime.UtcNow - _lastPopup >= TimeSpan.FromSeconds(60)) {
            _lastPopup = DateTime.UtcNow;
            EmMod.WarnWithPopup<IChatProvider>($"[{Alias}] {message}");
        } else {
            EmMod.Warn<IChatProvider>($"[{Id}] temporarily unavailable: {message}");
        }
    }

    public virtual void UpdateAvailability()
    {
        if (DateTime.UtcNow >= _cooldownUntil) {
            _cooldownUntil = DateTime.MinValue;
        }
    }

    public UniTask<ChatMessageContent> HandleRequest(Kernel kernel,
                                                     ChatHistory context,
                                                     EmActivity activity,
                                                     bool rawOutput,
                                                     CancellationToken token)
    {
        return HandleRequestWithSchema(kernel, context, activity, rawOutput, null, token);
    }

    public virtual void MergeExtensionRequest(IDictionary<string, object> data, HttpRequestMessage request)
    {
        var (_, raw, schema) = CurrentScope;

        foreach (var (k, v) in RequestParams) {
            if (k.IsEmptyOrNull || v is null) {
                continue;
            }

            if (raw && k is "response_format") {
                continue;
            }

            data[k] = v;
        }

        if (raw && schema is not null) {
            ApplyResponseSchema(data, schema);
        }
    }

    internal async UniTask<ChatMessageContent> HandleRequestWithSchema(Kernel kernel,
                                                             ChatHistory context,
                                                             EmActivity activity,
                                                             bool rawOutput,
                                                             JObject? responseSchema,
                                                             CancellationToken token)
    {
        activity.SetStatus(EmActivity.StatusType.InProgress);

        HandleRequestInternal();

        var service = kernel.GetRequiredService<IChatCompletionService>(Id);
        var settings = rawOutput ? RawExecutionSettings : ExecutionSettings;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeout));

        ChatMessageContent response;
        try {
            response = await SendScopedAsync(service, context, settings, kernel, this, rawOutput, responseSchema, cts.Token)
                .AsUniTask(false);
        } catch (OperationCanceledException) when (!token.IsCancellationRequested) {
            await UniTask.SwitchToMainThread();
            activity.SetStatus(EmActivity.StatusType.Timeout);
            throw;
        } catch {
            await UniTask.SwitchToMainThread();
            throw;
        }

        await UniTask.SwitchToMainThread();

        HandleRequestActivity(response, activity);

        _timeoutIncremental = 0f;

        return response;
    }

    protected virtual void ApplyResponseSchema(IDictionary<string, object> data, JObject schema)
    {
    }

    private static async Task<ChatMessageContent> SendScopedAsync(IChatCompletionService service,
                                                                  ChatHistory history,
                                                                  PromptExecutionSettings settings,
                                                                  Kernel kernel,
                                                                  IChatProvider provider,
                                                                  bool raw,
                                                                  JObject? schema,
                                                                  CancellationToken ct)
    {
        _scope.Value = (provider, raw, schema);
        return await service.GetChatMessageContentAsync(history, settings, kernel, ct).ConfigureAwait(false);
    }

    protected abstract void Register(IKernelBuilder builder, string model);

    protected abstract void HandleRequestActivity(ChatMessageContent response, EmActivity activity);

    protected abstract void HandleRequestInternal();
}