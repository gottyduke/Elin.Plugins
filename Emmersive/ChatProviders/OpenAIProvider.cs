using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Emmersive.API.Plugins;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using OpenAI.Chat;
using YKF;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Emmersive.ChatProviders;

internal class OpenAIProvider(string apiKey) : ChatProviderBase(apiKey)
{
    private UIInputText? _aliasInput;
    private UIInputText? _endpointInput;

    [field: AllowNull]
    public override string Id
    {
        get => field ??= $"{Alias}#{ServiceCount}";
        set;
    }

    public override string CurrentModel { get; set; } = "gpt-5-nano";

    [JsonIgnore]
    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["response_format"] = SceneReaction.OpenAiSchema,
    };

    public string EndPoint { get; set; } = "https://api.openai.com/v1";
    public string Alias { get; set; } = "OpenAI";

    [JsonIgnore]
    public override PromptExecutionSettings ExecutionSettings { get; set; } = new OpenAIPromptExecutionSettings {
        // as of 1.66.0 openai ResponseFormat cannot be set to a type or schema
        // which will cause serializer failure on WriteCore
        // DeepSeek does not use json schema either
        ReasoningEffort = "minimal",
        FrequencyPenalty = 0.6,
    };

    public override void OnLayoutConfirm()
    {
        if (_endpointInput != null) {
            EndPoint = _endpointInput.Text;
        }

        if (_aliasInput != null) {
            Id = Id.Replace(Alias, _aliasInput.Text);
            Alias = _aliasInput.Text;
        }

        base.OnLayoutConfirm();
    }

    protected override void OnLayoutInternal(YKLayout card)
    {
        _endpointInput = card.AddPair("em_ui_endpoint".lang(), EndPoint);
        _aliasInput = card.AddPair("em_ui_alias".lang(), Alias);
    }

    protected override void HandleRequestInternal(ChatMessageContent response, EmActivity activity)
    {
        if (response is not OpenAIChatMessageContent message) {
            return;
        }

        if (message.Metadata?.GetValueOrDefault("Usage") is not ChatTokenUsage usage) {
            return;
        }

        activity.InputToken = usage.InputTokenCount;
        activity.OutputToken = usage.OutputTokenCount;
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
        builder.AddOpenAIChatCompletion(CurrentModel, new Uri(EndPoint), ApiKey, serviceId: Id);
    }
}