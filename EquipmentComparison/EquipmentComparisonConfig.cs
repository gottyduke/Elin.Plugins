using BepInEx.Configuration;
using UnityEngine;

namespace EC;

internal static class EquipmentComparisonConfig
{
    internal static ConfigEntry<KeyCode>? Toggle;
    internal static ConfigEntry<KeyCode>? Modifier;

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
    }
}