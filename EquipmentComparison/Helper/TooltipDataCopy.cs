using BepInEx;
using EC.Components;

namespace EC.Helper;

internal static class TooltipDataCopy
{
    internal static TooltipData CopyTooltipWithId(this ButtonGridDrag grid, string idOverride = "")
    {
        var data = grid.tooltip;
        return new() {
            enable = data.enable,
            icon = data.icon,
            offset = data.offset,
            id = idOverride.IsNullOrWhiteSpace() ? data.id : idOverride,
            lang = data.lang,
            text = data.text,
            onShowTooltip = t => AuxTooltip.SetTooltipOverride(grid.card, t),
        };
    }
}