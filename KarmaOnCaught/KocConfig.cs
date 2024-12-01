using BepInEx.Configuration;

namespace KoC;

internal class KocConfig
{
    internal static ConfigEntry<bool>? PatchSteal;
    internal static ConfigEntry<bool>? PatchLockpick;
    internal static ConfigEntry<bool>? PatchHarvest;
    internal static ConfigEntry<bool>? PatchDwarf;
    internal static ConfigEntry<int>? DetectionRadius;

    internal static void LoadConfig(ConfigFile config)
    {
        PatchSteal = config.Bind(
            ModInfo.Name,
            "Patch Steal",
            true,
            "Pickpocket; 顺手牵羊");
        PatchLockpick = config.Bind(
            ModInfo.Name,
            "Patch Lockpick",
            true,
            "Lockpick; 取之有道");
        PatchHarvest = config.Bind(
            ModInfo.Name,
            "Patch Harvest",
            true,
            "Harvest; 俺拾走咧");
        PatchDwarf = config.Bind(
            ModInfo.Name,
            "Patch Dwarf",
            true,
            "Dig n mine, rock n stone; 矮人行为");
        DetectionRadius = config.Bind(
            ModInfo.Name,
            "Detection Radius",
            4,
            "In tiles; 目击范围");
    }
}