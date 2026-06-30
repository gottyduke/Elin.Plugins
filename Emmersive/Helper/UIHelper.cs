using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Helper;

public static class UIHelper
{
    private static readonly Dictionary<string, Sprite> _lookup = [];

    public static Sprite? FindSprite(string path, string name)
    {
        if (!_lookup.TryGetValue(name, out var sprite)) {
            sprite = _lookup[name] = Resources.LoadAll<Sprite>(path)?.FirstOrDefault(s => s.name == name)!;
        }

        return sprite;
    }

    extension<T>(T layout) where T : YKLayout
    {
        public T FlexWidth()
        {
            layout.Spacer(0).LayoutElement().flexibleWidth = 1f;
            return layout;
        }

        public T FlexHeight()
        {
            layout.Spacer(0).LayoutElement().flexibleHeight = 1f;
            return layout;
        }

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

        public void ShowActivityInfo(string serviceName)
        {
            var summary = EmActivity.GetSummary(serviceName);
            if (summary.RequestTotal == 0) {
                return;
            }

            var card = layout.Horizontal();
            card.Layout.childForceExpandWidth = true;

            var left = card.Vertical();
            left.TopicDomain("em_ui_requests_total", $"{summary.RequestTotal:N0}");
            left.TopicDomain("em_ui_requests_success", $"{summary.RequestSuccess:N0}");
            left.TopicDomain("em_ui_requests_failed", $"{summary.RequestFailure:N0}");
            left.TopicDomain("em_ui_requests_rpm", $"{summary.RequestPerMin:N0}");
            left.TopicDomain("em_ui_avg_latency", $"{summary.LatencyAverage:N1}s");

            var right = card.Vertical();
            right.TopicDomain("em_ui_tokens_total", $"{summary.TokensTotal:N0}");
            right.TopicDomain("em_ui_tokens_input", $"{summary.TokensInput:N0}");
            right.TopicDomain("em_ui_tokens_tph", $"{summary.TokensLastHour:N0}");
            right.TopicDomain("em_ui_tokens_tpm", $"{summary.TokensPerMin:N0}");
            right.TopicDomain("em_ui_tokens_tpr", $"{summary.TokensPerRequest:N1}");

            card.Spacer(5);
        }
    }
}