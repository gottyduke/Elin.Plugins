﻿namespace EC;

internal static class EquipmentComparisonLoc
{
    internal static string EquippedIndicator => Lang.langCode switch {
        "CN" => "(已装备)",
        "JP" => "(装備中)",
        _ => "(Equipped)",
    };

    internal static string CarriedIndicator => Lang.langCode switch {
        "CN" => "(已携带)",
        "JP" => "(装備中)", // ? 持っている??
        _ => "(Carried)",
    };

    internal static string TogglePrompt(bool on)
    {
        return Lang.langCode switch {
            "CN" => "装备比较: " + (on ? "启用 " : "禁用 "),
            "JP" => "装備比較: " + (on ? "启用 " : "無効 "),
            _ => "Compare Equipment: " + (on ? "Enabled " : "Disabled "),
        };
    }
}