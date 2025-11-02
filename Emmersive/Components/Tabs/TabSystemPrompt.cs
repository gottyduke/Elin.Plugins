using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using Emmersive.Contexts;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabSystemPrompt : TabDebugPanel
{
    private readonly List<UIInputText> _filters = [];

    public override void OnLayout()
    {
        BuildPromptButtons();

        BuildPromptCard("em_ui_system_prompt", "Emmersive/SystemPrompt.txt");

        if (EClass.core.IsGameStarted) {
            BuildPromptCard("em_ui_zone".Loc(EClass._zone.Name), $"Emmersive/Zones/{EClass._zone.ZoneFullName}.txt");
        }

        BuildContextFilter();

        base.OnLayout();
    }

    public override void OnLayoutConfirm()
    {
        RecentActionContext.Filters = _filters
            .Select(i => i.Text)
            .ToHashSet();

        RecentActionContext.Filters.Remove("");

        base.OnLayoutConfirm();
    }

    internal void BuildPromptButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_hard_reset_prompts".lang(), () => {
            ResourceFetch.ClearActiveResources();
            RelationContext.Clear();
            LayerEmmersivePanel.Instance?.Reopen();
        }).GetComponent<Image>().color = Color.red;

        btnGroup.Button("em_ui_open_folder".lang(), () => OpenFileOrPath.Run(ResourceFetch.CustomFolder));
    }

    internal void BuildContextFilter()
    {
        var card = this.MakeCard();

        card.HeaderCard("em_ui_filter");

        foreach (var filter in RecentActionContext.Filters) {
            _filters.Add(AddFilterInput(filter));
        }

        card.Button("em_ui_add".lang(), () => {
            var filter = AddFilterInput("");
            filter.transform.parent.SetSiblingIndex(filter.transform.parent.GetSiblingIndex() - 1);
            _filters.Add(filter);
        });

        return;

        UIInputText AddFilterInput(string text)
        {
            var pair = card.Horizontal();
            pair.Layout.childForceExpandWidth = true;

            var input = pair.InputText(text);

            input.type = UIInputText.Type.Name;
            input.field.characterLimit = 150;
            input.field.contentType = InputField.ContentType.Standard;
            input.field.inputType = InputField.InputType.Standard;
            input.field.characterValidation = InputField.CharacterValidation.None;

            input.Text = text;

            pair.Button("em_ui_remove".lang(), () => DestroyImmediate(pair.gameObject));

            return input;
        }
    }
}