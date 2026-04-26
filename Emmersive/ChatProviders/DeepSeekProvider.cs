using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
public class DeepSeekProvider(string apiKey) : OpenAIProvider(apiKey)
{
    [JsonProperty]
    public override string Alias { get; set; } = "DeepSeek";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "deepseek-v4-flash";

    [JsonProperty]
    public override string EndPoint { get; set; } = "https://api.deepseek.com/v1";

    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["frequency_penalty"] = 0.6f,
        ["thinking"] = JObject.FromObject(new {
            type = "disabled",
        }),
        ["response_format"] = JObject.FromObject(new {
            type = "json_object",
        }),
    };
}