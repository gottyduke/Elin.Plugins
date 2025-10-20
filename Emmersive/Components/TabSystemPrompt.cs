using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using Emmersive.Contexts;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabSystemPrompt : TabEmmersiveBase
{
    public override void OnLayout()
    {
        BuildPromptButtons();

        BuildPromptCard("em_ui_system_prompt", "Emmersive/SystemPrompt.txt");

        if (EClass.core.IsGameStarted) {
            BuildPromptCard("em_ui_zone".Loc(EClass._zone.Name), $"Emmersive/Zones/{EClass._zone.ZoneFullName}.txt");
        }

        base.OnLayout();
    }

    internal void BuildPromptButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_hard_reset_prompts".lang(), () => {
            ResourceFetch.ClearActiveResources();
            RelationContext.Clear();
            LayerEmmersivePanel.Instance?.Reopen(name);
        }).GetComponent<Image>().color = Color.red;

        btnGroup.Button("em_ui_open_folder".lang(), () => OpenFileOrPath.Run(ResourceFetch.CustomFolder));
    }
}