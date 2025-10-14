using UnityEngine.UI;
using YKF;

namespace Emmersive.Helper;

public static class UIHelper
{
    extension(YKLayout layout)
    {
        public UIInputText AddPair(string idLang, string text)
        {
            var pair = layout.Horizontal();
            pair.Layout.childForceExpandWidth = true;

            pair.Text(idLang);
            var input = pair.InputText(text);

            input.type = UIInputText.Type.Name;
            input.field.characterLimit = 150;
            input.field.contentType = InputField.ContentType.Standard;
            input.field.inputType = InputField.InputType.Standard;
            input.field.characterValidation = InputField.CharacterValidation.None;

            input.Text = text;

            return input;
        }
    }
}