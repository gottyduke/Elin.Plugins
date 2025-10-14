using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.Services.SceneDirector;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Newtonsoft.Json;
using YKF;

namespace Emmersive.ChatProviders;

public sealed class GoogleProvider : ChatProviderBase
{
    [field: AllowNull]
    public override string Id
    {
        get => field ??= $"GoogleGemini#{ServiceCount}";
        set;
    }

    public override string CurrentModel { get; set; } = "gemini-2.5-flash";
    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object>();

    [JsonIgnore]
    public override PromptExecutionSettings ExecutionSettings { get; set; } = new GeminiPromptExecutionSettings {
        ResponseMimeType = "application/json",
        ResponseSchema = typeof(SceneReaction[]),
    };

    public override async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var response = await base.HandleRequest(kernel, context, token);
        if (response is not GeminiChatMessageContent { Metadata: { } metadata }) {
            return response;
        }

        var activity = EmActivity.Current;
        if (activity is not null) {
            activity.InputToken = metadata.PromptTokenCount;
            activity.OutputToken = metadata.CandidatesTokenCount;
        }

        return response;
    }

    public override void MergeExtensionData(IDictionary<string, object> data)
    {
        if (data.TryGetValue("generationConfig", out var geminiRequest) &&
            geminiRequest is Dictionary<string, object> generationConfig) {
            base.MergeExtensionData(generationConfig);
        }
    }

    protected override void OnLayoutInternal(YKLayout card)
    {
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddGoogleAIGeminiChatCompletion(model, ApiKey, serviceId: Id);
    }
}