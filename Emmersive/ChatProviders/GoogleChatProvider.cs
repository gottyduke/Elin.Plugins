using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Emmersive.ChatProviders;

public sealed class GoogleChatProvider(string apiKey) : ChatProviderBase
{
    [field: AllowNull]
    public override string Id => field ??= $"GoogleGemini#{ServiceCount}";

    public override IReadOnlyList<string> Models => [
        "gemini-2.5-pro",
        "gemini-2.5-flash",
    ];

    public override string CurrentModel { get; set; } = "gemini-2.5-flash";

    public override PromptExecutionSettings ExecutionSettings { get; set; } = new GeminiPromptExecutionSettings();

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddGoogleAIGeminiChatCompletion(model, apiKey, serviceId: Id);
    }

    public override async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var response = await base.HandleRequest(kernel, context, token);

        // don't count the round trip
        if (context.FirstOrDefault(c => c is { Metadata: GeminiMetadata })?.Metadata is not GeminiMetadata metadata) {
            return response;
        }

        EmActivity.Current?.Token = metadata.TotalTokenCount;

        return response;
    }
}