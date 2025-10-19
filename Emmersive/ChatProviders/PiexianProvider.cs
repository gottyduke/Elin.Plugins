using Emmersive.Helper;
using UnityEngine.UI;
using YKF;

namespace Emmersive.ChatProviders;

internal sealed class PiexianProvider : GoogleProvider
{
    private UIInputText? _apiInput;

    internal PiexianProvider()
        : base("")
    {
    }

    public override string Alias { get; set; } = "氕氙";
    public override string CurrentModel { get; set; } = "gemini-2.5-flash-nothinking";
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
}