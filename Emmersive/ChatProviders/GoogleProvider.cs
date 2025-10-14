using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.Services.SceneDirector;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using YKF;

namespace Emmersive.ChatProviders;

public sealed class GoogleProvider(string apiKey) : ChatProviderBase
{
    [field: AllowNull]
    public override string Id
    {
        get => field ??= $"GoogleGemini#{ServiceCount}";
        set;
    }

    public override IReadOnlyList<string> Models => [
        "gemini-2.5-pro",
        "gemini-2.5-flash",
    ];

    public override string CurrentModel { get; set; } = "gemini-2.5-flash";

    public override PromptExecutionSettings ExecutionSettings { get; set; } = new GeminiPromptExecutionSettings {
        ResponseSchema = SceneReaction.Schema,
        ResponseMimeType = "application/json",
    };

    public override void OnLayout(YKLayout layout)
    {
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddGoogleAIGeminiChatCompletion(model, apiKey, serviceId: Id);
    }

    public override async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var response = await base.HandleRequest(kernel, context, token);

        if (response is not GeminiChatMessageContent { Metadata: { } metadata } message) {
            return response;
        }

        var activity = EmActivity.Current;
        if (activity is not null) {
            activity.InputToken = metadata.PromptTokenCount;
            activity.OutputToken = metadata.CandidatesTokenCount;
        }

        return response;
    }
}