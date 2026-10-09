using System.Collections.Generic;
using Emmersive.API.Plugins;
using Newtonsoft.Json;
using YKF;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
internal sealed class PiexianProvider() : OpenAIProvider("")
{
    [JsonProperty]
    public override string Alias { get; set; } = "氕氙";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "gemini-3-flash";

    [JsonProperty]
    public override string EndPoint { get; set; } = "https://api.pie-xian.com/v1";

    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["response_format"] = SceneReaction.OpenAiSchema,
    };

    protected override void OnLayoutInternal(YKLayout card)
    {
    }

    protected override void HandleRequestInternal()
    {
        // piexian says no touching
    }
}