using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using Cwl.Helper.Exceptions;
using Cysharp.Threading.Tasks;
using Emmersive.API.Plugins.SceneDirector;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Emmersive.ChatProviders;

internal class OpenAIProvider(string apiKey, string endPoint = "https://api.openai.com/v1", string alias = "OpenAI")
    : ChatProviderBase
{
    [field: AllowNull]
    public override string Id => field ??= $"{alias}#{ServiceCount}";

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

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddOpenAIChatCompletion(CurrentModel, new Uri(endPoint), apiKey, serviceId: Id);
    }

    public override async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var response = await base.HandleRequest(kernel, context, token);

        if (response is not OpenAIChatMessageContent { Content: { } content } message) {
            return response;
        }

        if (message.Metadata?.GetValueOrDefault("Usage") is ChatTokenUsage usage) {
            EmActivity.Current?.Token = usage.TotalTokenCount;
        }

        SceneReaction[]? reactions;

        try {
            reactions = JsonSerializer.Deserialize<SceneReaction[]>(content);
        } catch (Exception ex) {
            return ThrowOrReturn.Return(ex, message);
            // noexcept
        }

        var director = kernel.GetRequiredService<SceneDirector>();

        foreach (var reaction in reactions ?? []) {
            director.DoPopText(reaction.uid, reaction.text, reaction.duration, reaction.delay);
        }

        return message;
    }
}