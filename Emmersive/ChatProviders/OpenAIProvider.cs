using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using YKF;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Emmersive.ChatProviders;

internal class OpenAIProvider(string apiKey) : ChatProviderBase
{
    [field: AllowNull]
    public override string Id
    {
        get => field ??= $"{Alias}#{ServiceCount}";
        set;
    }

    public override IReadOnlyList<string> Models => [
        "deepseek-chat",
        "gpt-5",
        "gpt-5-mini",
        "gpt-5-nano",
    ];

    public override string CurrentModel { get; set; } = "gpt-5-nano";

    public override PromptExecutionSettings ExecutionSettings { get; set; } = new OpenAIPromptExecutionSettings {
        // use no-brainer to reduce latency
        ReasoningEffort = "minimal",
        Temperature = 1f,
        // as of 1.66.0 openai ResponseFormat cannot be set to a type or schema
        // which will cause serializer failure on WriteCore
        // DeepSeek does not use json schema either
        ResponseFormat = new {
            type = "json_object",
        },
    };

    public string EndPoint { get; set; } = "https://api.openai.com/v1";
    public string Alias { get; set; } = "OpenAI";

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddOpenAIChatCompletion(CurrentModel, new Uri(EndPoint), apiKey, serviceId: Id);
    }

    public override async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var response = await base.HandleRequest(kernel, context, token);

        if (response is not OpenAIChatMessageContent message) {
            return response;
        }

        var activity = EmActivity.Current;
        if (activity is not null) {
            if (message.Metadata?.GetValueOrDefault("Usage") is ChatTokenUsage usage) {
                activity.InputToken = usage.InputTokenCount;
                activity.OutputToken = usage.OutputTokenCount;
            }
        }

        return message;
    }

    public override void OnLayout(YKLayout layout)
    {
        base.OnLayout(layout);
    }
}