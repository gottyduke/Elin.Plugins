using Newtonsoft.Json;

namespace Emmersive.ChatProviders;

public class OllamaProvider() : OpenAIProvider("")
{
    [JsonProperty]
    public override string Alias { get; set; } = "Ollama";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "";

    [JsonProperty]
    public override string EndPoint => "http://127.0.0.1:11434/v1";
}