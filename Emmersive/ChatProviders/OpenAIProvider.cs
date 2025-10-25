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

[JsonObject(MemberSerialization.OptIn)]
public class OpenAIProvider(string apiKey) : ChatProviderBase(apiKey)
{
    [JsonProperty]
    public override string Alias { get; set; } = "OpenAI";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "gpt-5-nano";

    [JsonProperty]
    public override string EndPoint { get; set; } = "https://api.openai.com/v1";

    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["frequency_penalty"] = 0.6f,
        ["reasoning_effort"] = "minimal",
        ["response_format"] = SceneReaction.OpenAiSchema,
    };

    public override PromptExecutionSettings ExecutionSettings { get; set; } = new OpenAIPromptExecutionSettings {
        // as of 1.66.0 openai ResponseFormat cannot be set to a type or schema
        // which will cause serializer failure on WriteCore
        // DeepSeek does not use json schema either
        ResponseFormat = "minimal",
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