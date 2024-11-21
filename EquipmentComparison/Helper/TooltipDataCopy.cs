using BepInEx;

namespace EC.Helper;

internal static class TooltipDataCopy
{
    internal static TooltipData CopyTooltipWithId(this ButtonGridDrag grid, string idOverride = "",
        string headerPrefix = "")
    {
        var data = grid.tooltip;
        return new() {
            enable = data.enable,
            icon = data.icon,
            offset = data.offset,
            id = idOverride.IsNullOrWhiteSpace() ? data.id : idOverride,
            lang = data.lang,
            text = data.text,
            onShowTooltip = t => SetTooltipOverride(grid.card, t, headerPrefix),
        };
    }

    private static void SetTooltipOverride(Card card, UITooltip tooltip, string prefix)
    {
        card.WriteNote(tooltip.note, n => {
            var header = n.GetComponentInChildren<UIText>();
            header.text = $"{prefix} {header.text}";
        });
    }
}