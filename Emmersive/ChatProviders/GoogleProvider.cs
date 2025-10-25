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
        ["thinkingConfig"] = JObject.FromObject(new {
            thinkingBudget = 0,
        }),
    };

    public override PromptExecutionSettings ExecutionSettings { get; set; } = new GeminiPromptExecutionSettings {
        ResponseMimeType = "application/json",
        ResponseSchema = typeof(SceneReaction[]),
        ThinkingConfig = new() {
            ThinkingBudget = 0,
        },
    };

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

        base.MergeExtensionRequest(generationConfig, request);
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
        (ExecutionSettings as GeminiPromptExecutionSettings)?.ThinkingConfig?.ThinkingBudget =
            CurrentModel.EndsWith("pro") ? 128 : 0;
    }
}