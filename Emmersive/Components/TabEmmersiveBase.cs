using Cwl.Helper.String;
using Emmersive.Helper;
using YKF;

namespace Emmersive.Components;

internal abstract class TabEmmersiveBase : YKLayout<LayerCreationData>
{
    public abstract void OnLayoutConfirm();

    internal YKLayout BuildPromptCard(string idLang, string path)
    {
        var card = this.MakeCard();

        var titleGroup = card.Horizontal();
        titleGroup.Layout.childForceExpandWidth = true;

        titleGroup.HeaderCard(idLang);

        var btnGroup = titleGroup.Horizontal();
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_reset".lang(), () => {
            ResourceFetch.RemoveCustomResource(path);
            ResourceFetch.RemoveActiveResource(path);
            OnLayoutConfirm();
        });

        btnGroup.Button("em_ui_edit".lang(), () => ResourceFetch.OpenOrCreateCustomResource(path));

        card.Spacer(5);

        var truncated = ResourceFetch.GetActiveResource(path).Truncate(400);
        card.Text(truncated.IsEmpty("em_ui_non_provided"));

        return card;
    }
}