using BepInEx.Configuration;
using UnityEngine;

namespace KoC;

internal class KocConfig
{
    internal static ConfigEntry<bool>? PatchSteal;
    internal static ConfigEntry<bool>? PatchUnlock;
    internal static ConfigEntry<bool>? PatchHarvest;
    internal static ConfigEntry<bool>? PatchDig;
    internal static ConfigEntry<bool>? PatchMine;

    internal static void LoadConfig(ConfigFile config)
    {
        PatchSteal = config.Bind(
            ModInfo.Name, 
            "Patch Steal", 
            true, 
            "Stealing/Pickpocketing");
        PatchUnlock = config.Bind(
            ModInfo.Name, 
            "Patch Unlock", 
            true, 
            "Unlocking");
        PatchHarvest = config.Bind(
            ModInfo.Name, 
            "Patch Harvest", 
            true, 
            "Harvesting");
        PatchDig = config.Bind(
            ModInfo.Name, 
            "Patch Dig", 
            true, 
            "Diggididigi");
        PatchMine = config.Bind(
            ModInfo.Name, 
            "Patch Mine", 
            true, 
            "Mining");
    }
}