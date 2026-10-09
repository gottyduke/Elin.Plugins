using System;
using System.Collections.Generic;
using System.Net.Http;
using Emmersive.API.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YKF;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
public class GoogleProvider(string apiKey) : ChatProviderBase(apiKey)
{
    private const string DefaultGoogleV1Beta = "https://generativelanguage.googleapis.com/v1beta";

    [JsonProperty]
    public override string Alias { get; set; } = "GoogleGemini";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "gemini-2.5-flash";

    [JsonProperty]
    public override string EndPoint { get; set; } = DefaultGoogleV1Beta;

    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["topP"] = 0.9f,
        ["temperature"] = 0.9f,
    };

    public override PromptExecutionSettings ExecutionSettings { get; set; } = new GeminiPromptExecutionSettings {
        ResponseMimeType = "application/json",
        ResponseSchema = SceneReaction.KernelSchema,
        ThinkingConfig = new() {
            ThinkingBudget = 0,
        },
    };

    protected override PromptExecutionSettings RawExecutionSettings =>
        field ??= new GeminiPromptExecutionSettings {
            ResponseMimeType = "application/json",
            ThinkingConfig = new() {
                ThinkingBudget = 0,
            },
        };

    private bool UsesThinking => CurrentModel.Contains("pro", StringComparison.OrdinalIgnoreCase);

    public override void MergeExtensionRequest(IDictionary<string, object> data, HttpRequestMessage request)
    {
        if (!data.TryGetValue("generationConfig", out var geminiRequest) ||
            geminiRequest is not IDictionary<string, object> generationConfig) {
            return;
        }

        var uri = request.RequestUri.ToString();
        if (uri.StartsWith(DefaultGoogleV1Beta, StringComparison.InvariantCultureIgnoreCase) &&
            !DefaultGoogleV1Beta.Equals(EndPoint, StringComparison.InvariantCultureIgnoreCase)) {
            request.RequestUri = new(EndPoint + uri[DefaultGoogleV1Beta.Length..]);
        }

        generationConfig.TryGetValue("thinkingConfig", out var thinking);

        base.MergeExtensionRequest(generationConfig, request);

        if (UsesThinking) {
            if (thinking is null) {
                generationConfig.Remove("thinkingConfig");
            } else {
                generationConfig["thinkingConfig"] = thinking;
            }
        }
    }

    protected override void ApplyResponseSchema(IDictionary<string, object> data, JObject schema)
    {
        data["responseMimeType"] = "application/json";
        data["responseSchema"] = schema;
    }

    protected override void OnLayoutInternal(YKLayout card)
    {
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddGoogleAIGeminiChatCompletion(model, ApiKey, serviceId: Id);
    }

    protected override void HandleRequestActivity(ChatMessageContent response, EmActivity activity)
    {
        if (response is not GeminiChatMessageContent { Metadata: { } metadata }) {
            activity.SetStatus(EmActivity.StatusType.Failed);
            return;
        }

        activity.TokensInput = metadata.PromptTokenCount;
        activity.TokensOutput = metadata.CandidatesTokenCount;
    }

    protected override void HandleRequestInternal()
    {
        var budget = UsesThinking ? 128 : 0;

        (ExecutionSettings as GeminiPromptExecutionSettings)?.ThinkingConfig?.ThinkingBudget = budget;
        (RawExecutionSettings as GeminiPromptExecutionSettings)?.ThinkingConfig?.ThinkingBudget = budget;
    }
}