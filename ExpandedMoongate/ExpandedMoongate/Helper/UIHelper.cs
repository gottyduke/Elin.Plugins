using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace EGate.Helper;

public static class UIHelper
{
    private static readonly Dictionary<string, Sprite> _lookup = [];

    public static Sprite? FindSprite(string path, string name)
    {
        if (!_lookup.TryGetValue(name, out var sprite)) {
            sprite = _lookup[name] = Resources.LoadAll<Sprite>(path)?
                .FirstOrDefault(s => s.name == name)!;
        }

        return sprite;
    }

    extension<T>(T layout) where T : YKLayout
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
            cardBg.sprite = FindSprite("UI/Window/Base/Asset/_uiset default", "buttonBig");
            cardBg.type = Image.Type.Sliced;

            return card;
        }

        public YKHorizontal MakeEqualWidthGroup()
        {
            var group = layout.Horizontal();
            var le = group.LayoutElement();
            le.minWidth = 0;
            le.preferredWidth = 0;
            le.flexibleWidth = 1;
            return group;
        }

        public Image AddImageCard(Component parent, Sprite sprite)
        {
            var bg = Util.Instantiate<UIItem>("UI/Element/Deco/ImageNote", parent).image1;
            bg.sprite = sprite;
            if (bg.sprite != null) {
                bg.SetNativeSize();

                var rt = bg.rectTransform;
                (rt.parent as RectTransform)?.sizeDelta = rt.sizeDelta;
            }
            return bg;
        }
    }
}