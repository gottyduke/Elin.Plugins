using Emmersive.Helper;
using Newtonsoft.Json;
using UnityEngine.UI;
using YKF;

namespace Emmersive.ChatProviders;

[JsonObject(MemberSerialization.OptIn)]
internal sealed class PiexianProvider : GoogleProvider
{
    private UIInputText? _apiInput;

    internal PiexianProvider()
        : base("")
    {
    }

    [JsonProperty]
    public override string Alias { get; set; } = "氕氙";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "gemini-2.5-flash";

    [JsonProperty]
    public override string EndPoint { get; set; } = "https://proxy.pieixan.icu/v1beta";

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