using Emmersive.API.Services;
using Emmersive.Helper;
using EModding.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal abstract class TabEmmersiveBase : YKLayout<LayerCreationData>
{
    public virtual void OnLayoutConfirm()
    {
    }

    internal YKLayout BuildPromptCard(string idLang, string path)
    {
        var card = this.MakeCard();

        var titleGroup = card.Horizontal();
        titleGroup.Layout.childForceExpandWidth = true;

        titleGroup.HeaderCard(idLang).LayoutElement().preferredWidth = 600f;

        var btnGroup = titleGroup.Horizontal();
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_reset".lang(), () => {
            ResourceFetch.RemoveCustomResource(path);
            ResourceFetch.RemoveActiveResource(path);
            OnLayoutConfirm();
        }).GetComponent<Image>().color = Color.red;

        btnGroup.Button("em_ui_edit".lang(), () => ResourceFetch.OpenOrCreateCustomResource(path));

        card.Spacer(5);

        var truncated = ResourceFetch.GetActiveResource(path).Truncate(400);
        card.Text(truncated.OrIfEmpty("em_ui_non_provided"));

        return card;
    }

    internal static Vector2 FitCell(int constraint)
    {
        var scaler = EMono.ui.canvasScaler.scaleFactor;
        return new Vector2(Screen.width / 1.7f / constraint, 45f) / scaler;
    }
}