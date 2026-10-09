using System.IO;
using System.Linq;
using Emmersive.API.Services;
using Emmersive.Contexts;
using Emmersive.Helper;
using Emmersive.LangMod;
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

    internal static Chara[] GetMapCharas(bool importantOnly = true)
    {
        return [
            EClass.pc,
            ..EClass._map.charas
                .Where(c => importantOnly ? c.Profile.IsImportant : !c.IsPC)
                .Distinct(UniqueCardComparer.Default)
                .OfType<Chara>()
                .OrderByDescending(c => c.IsPCFaction),
        ];
    }

    internal YKLayout BuildPromptCard(string idLang, string path)
    {
        var card = this.MakeCard();

        var titleGroup = card.Horizontal();
        titleGroup.Layout.childForceExpandWidth = true;

        titleGroup.HeaderCard(idLang).LayoutElement().preferredWidth = 600f;

        var btnGroup = titleGroup.Horizontal();
        btnGroup.Layout.childForceExpandWidth = true;

        var reset = btnGroup.Button("em_ui_reset".lang(), () => UIHelper.ConfirmDanger("em_ui_confirm_reset_prompt", () => {
            ResourceFetch.RemoveCustomResource(path);
            ResourceFetch.RemoveActiveResource(path);
            RelationContext.Clear();
            LayerEmmersivePanel.Instance?.Reopen();
        }, "em_ui_reset"));
        reset.GetComponent<Image>().color = Color.red;

        var custom = new FileInfo(ResourceFetch.CustomFolder + path);
        reset.SetInteractableWithAlpha(custom.Exists);

        btnGroup.Button("em_ui_edit".lang(), () => {
            ResourceFetch.OpenOrCreateCustomResource(path);
            reset.SetInteractableWithAlpha(true);
        });

        card.Spacer(5);

        var builtin = PackageIterator.GetFile(path, ModInfo.Guid);
        if (custom.Exists && builtin is { Exists: true } && builtin.LastWriteTime > custom.LastWriteTime) {
            card.Text("em_ui_prompt_outdated".Loc(builtin.LastWriteTime.ToString("yyyy-MM-dd")), FontColor.Warning);
        }

        var truncated = ResourceFetch.GetActiveResource(path).Truncate(400);
        card.Text(truncated.OrIfEmpty("em_ui_non_provided"));

        return card;
    }

    internal static Vector2 FitCell(int constraint)
    {
        var width = Screen.width / 1.7f / constraint / EMono.ui.canvasScaler.scaleFactor;
        return new(width, 45f);
    }
}