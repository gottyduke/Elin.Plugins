using System;
using Emmersive.Helper;
using Newtonsoft.Json;

namespace Emmersive.ChatProviders;

public class OllamaProvider() : OpenAIProvider("")
{
    [JsonProperty]
    public override string Alias { get; set; } = "Ollama";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "";

    [JsonProperty]
    public override string EndPoint { get; set; } = "http://127.0.0.1:11434/v1";

    protected override bool RequiresApiKey => false;

    protected override bool IsConfigured => base.IsConfigured && !CurrentModel.IsEmptyOrNull;

    protected override float RequestTimeout => Math.Max(60f, base.RequestTimeout);
}