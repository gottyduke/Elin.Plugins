using Emmersive.API.Plugins;
using Emmersive.Helper;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using YKF;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
internal sealed class PiexianProvider : OpenAIProvider
{
    private UIInputText? _apiInput;

    internal PiexianProvider()
        : base("")
    {
    }

    [JsonProperty]
    public override string Alias { get; set; } = "氕氙";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "gpt-4o-mini";

    [JsonProperty]
    public override string EndPoint { get; set; } = "https://proxy.pieixan.icu/v1";

    public override IDictionary<string, object> RequestParams { get; set; } = new Dictionary<string, object> {
        ["response_format"] = SceneReaction.OpenAiSchema,
    };

    protected override void OnLayoutInternal(YKLayout card)
    {
        _apiInput = card.AddPair("em_ui_api_key", ApiKey);
        _apiInput.field.inputType = InputField.InputType.Password;
    }

    public override void OnLayoutConfirm()
    {
        if (_apiInput != null) {
            ApiKey = _apiInput.Text;
        }

        base.OnLayoutConfirm();
    }

    protected override void HandleRequestInternal()
    {
        // piexian says no touching
    }
}