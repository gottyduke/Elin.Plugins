using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace ElinTogether.Helper;

public static class UIHelper
{
    private static readonly Dictionary<string, Sprite> _lookup = [];

    public static Sprite FindSprite(string name)
    {
        if (_lookup.TryGetValue(name, out var sprite)) {
            return sprite;
        }

        sprite = Resources.FindObjectsOfTypeAll<Sprite>()
            .LastOrDefault(s => s.name == name);

        return _lookup[name] = sprite!;
    }

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

        public YKVertical MakeCard()
        {
            var card = layout.Vertical();
            card.LayoutElement().flexibleWidth = 1f;
            card.Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.Layout.padding = new(20, 20, 20, 20);

            var cardBg = card.Layout.gameObject.AddComponent<Image>();
            cardBg.sprite = FindSprite("buttonBig");
            cardBg.type = Image.Type.Sliced;

            return card;
        }

        public UIItem AddImageCard(Component parent, Sprite sprite)
        {
            var item = Util.Instantiate<UIItem>("UI/Element/Deco/ImageNote", parent);
            var bg = item.image1;
            var rt = bg.rectTransform;

            bg.sprite = sprite;
            if (bg.sprite != null) {
                bg.SetNativeSize();
                (rt.parent as RectTransform)?.sizeDelta = rt.sizeDelta;
            }
            return item;
        }
    }
}