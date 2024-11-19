using BepInEx.Configuration;
using UnityEngine;

namespace EC;

internal static class EquipmentComparisonConfig
{
    internal static ConfigEntry<KeyCode>? Toggle;
    internal static ConfigEntry<KeyCode>? Modifier;
    internal static ConfigEntry<int>? MaxAuxNotes;

    internal static void LoadConfig(ConfigFile config)
    {
        Toggle = config.Bind(
            ModInfo.Name,
            "Toggle Key",
            KeyCode.C,
            "Toggle key; 装备比较 开关键"
        );
        Modifier = config.Bind(
            ModInfo.Name,
            "Modifier Key",
            KeyCode.LeftShift,
            "Whether or not to use a modifier key with toggle key; 设置额外的修改键(Shift/Ctrl之类)"
        );
        MaxAuxNotes = config.Bind(
            ModInfo.Name,
            "Max Comparable Tooltips",
            2,
            new ConfigDescription("Max amount of possible comparable tooltips to display at the same time; Note that too many will possibly be out of screen",
                new AcceptableValueRange<int>(0, 6))
        );
    }
}