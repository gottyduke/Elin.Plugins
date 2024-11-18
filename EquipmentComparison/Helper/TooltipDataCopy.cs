using BepInEx;

namespace Ec.Helper;

internal static class TooltipDataCopy
{
    internal static TooltipData CopyWithId(this TooltipData data, string idOverride = "")
    {
        return new() {
            enable = data.enable,
            icon = data.icon,
            offset = data.offset,
            id = idOverride.IsNullOrWhiteSpace() ? data.id : idOverride,
            lang = data.lang,
            text = data.text,
            onShowTooltip = data.onShowTooltip,
        };
    }
}