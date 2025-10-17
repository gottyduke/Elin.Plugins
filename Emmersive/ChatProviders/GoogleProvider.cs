using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Emmersive.API.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Newtonsoft.Json;
using YKF;

namespace Emmersive.ChatProviders;

public sealed class GoogleProvider(string apiKey) : ChatProviderBase(apiKey)
{
    [field: AllowNull]
    public override string Id
    {
        get => field ??= $"GoogleGemini#{ServiceCount}";
        set;
    }

    public override string CurrentModel { get; set; } = "gemini-2.5-flash";

    [JsonIgnore]
    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        //
    };

    [JsonIgnore]
    public override PromptExecutionSettings ExecutionSettings { get; set; } = new GeminiPromptExecutionSettings {
        ResponseMimeType = "application/json",
        ResponseSchema = typeof(SceneReaction[]),
        ThinkingConfig = new() {
            ThinkingBudget = 0,
        },
    };

    public override void MergeExtensionData(IDictionary<string, object> data)
    {
        if (!data.TryGetValue("generationConfig", out var geminiRequest) ||
            geminiRequest is not IDictionary<string, object> generationConfig) {
            return;
        }

        base.MergeExtensionData(generationConfig);
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
            return;
        }

        activity.InputToken = metadata.PromptTokenCount;
        activity.OutputToken = metadata.CandidatesTokenCount;
    }

    protected override void HandleRequestInternal()
    {
        (ExecutionSettings as GeminiPromptExecutionSettings)?.ThinkingConfig?.ThinkingBudget =
            CurrentModel.EndsWith("pro") ? 128 : 0;
    }
}