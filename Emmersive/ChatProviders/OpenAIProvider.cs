using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Emmersive.API.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    };

    protected override bool IsConfigured => TryGetEndpoint(out _);

    protected override void OnLayoutInternal(YKLayout card)
    {
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
        if (!TryGetEndpoint(out var uri)) {
            MarkUnavailable("em_ui_err_endpoint".lang());
            return;
        }

        builder.AddOpenAIChatCompletion(model, uri, ApiKey, serviceId: Id);
    }

    private bool TryGetEndpoint([NotNullWhen(true)] out Uri? uri)
    {
        return Uri.TryCreate(EndPoint, UriKind.Absolute, out uri) && uri.Scheme is "http" or "https";
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

    protected override void ApplyResponseSchema(IDictionary<string, object> data, JObject schema)
    {
        if (RequestParams.TryGetValue("response_format", out var format) &&
            format is JObject json && json["type"]?.ToString() == "json_object") {
            data["response_format"] = format;
            return;
        }

        data["response_format"] = JObject.FromObject(new {
            type = "json_schema",
            json_schema = new {
                name = "em_custom",
                strict = false,
                schema,
            },
        });
    }
}