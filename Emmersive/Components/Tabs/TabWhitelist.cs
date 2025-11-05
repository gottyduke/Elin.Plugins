using System;
using System.Collections.Generic;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using Emmersive.Helper;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabWhitelist : TabCharaRelations
{
    private UIButton? _whitelistMode;

    public override void OnLayout()
    {
        var header = Horizontal();
        header.Layout.childForceExpandWidth = true;

        header.Header("em_ui_chara_map");

        var isWhitelist = EmConfig.Context.WhitelistMode.Value;

        _whitelistMode = header.Toggle(
                GetModeText(),
                isWhitelist,
                value => {
                    EmConfig.Context.WhitelistMode.Value = value;
                    _whitelistMode?.mainText.text = GetModeText();
                    LayerEmmersivePanel.Instance?.Reopen();
                })
            .WithMinWidth(240);

        var mapCharas = GetMapCharas();

        BuildList(
            isWhitelist ? "em_ui_active_whitelist" : "em_ui_active_blacklist",
            mapCharas,
            isWhitelist ? "em_wl" : "em_bl",
            c => isWhitelist ? c.Profile.OnWhitelist : c.Profile.OnBlacklist);

        return;

        void BuildList(string listName, IEnumerable<Chara> charas, string flagKey, Func<Chara, bool> getInitial)
        {
            var generator = this.MakeCard();

            generator.TextFlavor(listName);

            var grid = generator.Grid()
                .WithConstraintCount(2);
            grid.Fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            grid.Layout.cellSize = FitCell(2);

            foreach (var chara in charas) {
                var initial = getInitial(chara);
                grid.Toggle(chara.Name, initial, value => chara.SetFlagValue(flagKey, value ? 1 : 0));
            }
        }

        string GetModeText()
        {
            var isOn = EmConfig.Context.WhitelistMode.Value;
            return "em_ui_whitelist".Loc((isOn ? "on" : "off").lang());
        }
    }
}