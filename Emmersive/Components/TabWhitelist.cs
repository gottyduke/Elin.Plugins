using System.Linq;
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
        _whitelistMode = header.Toggle(
            GetCurrentWhitelistMode(),
            EmConfig.Context.WhitelistMode.Value,
            value => {
                EmConfig.Context.WhitelistMode.Value = value;
                _whitelistMode?.mainText.text = GetCurrentWhitelistMode();
            });

        var generator = this.MakeCard();

        var charas = GetMapCharas()
            .ToDictionary(c => c, c => c.Profile.OnWhitelist);

        var list = generator.Grid()
            .WithConstraintCount(2);
        list.Fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        list.Layout.cellSize = FitCell(2);

        foreach (var chara in GetMapCharas()) {
            list.Toggle(chara.Name, charas[chara], value => {
                charas[chara] = value;
                chara.SetFlagValue("em_wl", value ? 1 : 0);
            });
        }

        return;

        string GetCurrentWhitelistMode()
        {
            var isOn = EmConfig.Context.WhitelistMode.Value;
            return "em_ui_whitelist".Loc((isOn ? "on" : "off").lang());
        }
    }
}