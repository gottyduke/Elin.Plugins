using System;
using System.Collections.Generic;
using Emmersive.API.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using OpenAI.Chat;
using YKF;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Emmersive.ChatProviders;

public class OpenAIProvider(string apiKey) : ChatProviderBase(apiKey)
{
    public override string Alias { get; set; } = "OpenAI";
    public override string CurrentModel { get; set; } = "gpt-5-nano";
    public override string EndPoint { get; set; } = "https://api.openai.com/v1";

    [JsonIgnore]
    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["response_format"] = SceneReaction.OpenAiSchema,
    };

    [JsonIgnore]
    public override PromptExecutionSettings ExecutionSettings { get; set; } = new OpenAIPromptExecutionSettings {
        // as of 1.66.0 openai ResponseFormat cannot be set to a type or schema
        // which will cause serializer failure on WriteCore
        // DeepSeek does not use json schema either
        ReasoningEffort = "minimal",
        FrequencyPenalty = 0.6,
    };

    protected override void OnLayoutInternal(YKLayout card)
    {
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddOpenAIChatCompletion(CurrentModel, new Uri(EndPoint), ApiKey, serviceId: Id);
    }

    protected override void HandleRequestActivity(ChatMessageContent response, EmActivity activity)
    {
        if (response is not OpenAIChatMessageContent message) {
            return;
        }

        if (message.Metadata?.GetValueOrDefault("Usage") is not ChatTokenUsage usage) {
            return;
        }

        activity.TokensInput = usage.InputTokenCount;
        activity.TokensOutput = usage.OutputTokenCount;
    }

    protected override void HandleRequestInternal()
    {
    }
}