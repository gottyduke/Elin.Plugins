using System.Collections.Generic;
using System.Linq;
using Emmersive.API.Services;
using Emmersive.Contexts;
using Emmersive.Helper;
using Emmersive.LangMod;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabSystemPrompt : TabEmmersiveBase
{
    private readonly List<UIInputText> _filters = [];

    public override void OnLayout()
    {
        BuildPromptButtons();

        BuildPromptCard("em_ui_system_prompt", "Emmersive/SystemPrompt.txt");

        if (EClass.core.IsGameStarted) {
            var zone = EClass._zone;
            var zonePrompt = $"Emmersive/Zones/{zone.ZoneFullName}.txt";
            var genericPrompt = $"Emmersive/Zones/Zone_{zone.id}.txt";
            if (ResourceFetch.GetActiveResource(zonePrompt).IsEmptyOrNull &&
                !ResourceFetch.GetActiveResource(genericPrompt).IsEmptyOrNull) {
                zonePrompt = genericPrompt;
            }

            BuildPromptCard("em_ui_zone".Loc(zone.Name), zonePrompt);
        }

        BuildContextFilter();
    }

    public override void OnLayoutConfirm()
    {
        RecentActionContext.Filters = [
            .._filters
                .Select(i => i.Text),
        ];

        RecentActionContext.Filters.Remove("");
        RecentActionContext.SaveFilters();

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
        });

        btnGroup.Button("em_ui_open_folder".lang(), () => Util.Run(ResourceFetch.CustomFolder));
    }

    internal void BuildContextFilter()
    {
        var card = this.MakeCard();

        card.HeaderCard("em_ui_filter");

        foreach (var filter in RecentActionContext.Filters) {
            AddFilterInput(filter);
        }

        card.Button("em_ui_add".lang(), () => {
            var row = AddFilterInput("");
            row.transform.SetSiblingIndex(row.transform.GetSiblingIndex() - 1);
        });

        return;

        YKHorizontal AddFilterInput(string text)
        {
            var row = card.Horizontal();
            var input = row.PlainTextInput(text);
            _filters.Add(input);

            row.Button("×", () => {
                _filters.Remove(input);
                DestroyImmediate(row.gameObject);
            }).GetOrCreate<Image>().color = Color.red;

            return row;
        }
    }
}