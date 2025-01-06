using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace KoC;

internal class KocConfig
{
    internal static readonly Dictionary<string, Patch> Managed = [];

    internal static void LoadConfig(ConfigFile config)
    {
        Managed["Steal"] = new("Steal", "Pickpocketing/Stealing; 顺手牵羊");
        Managed["Lockpick"] = new("Lockpick", "Lockpicking; 取之有道");
        Managed["Dwarf"] = new("Dwarf", "Digging n mining, rock n stone; 矮人行为");
        Managed["Harvest"] = new("Harvest", "Harvesting; 俺拾走咧");

        Managed.Values.Do(c => c.Bind(config));
    }

    internal class Patch(string key, string description)
    {
        internal ConfigEntry<bool>? Enabled { get; private set; }
        internal ConfigEntry<int>? DetectionRadius { get; private set; }
        internal ConfigEntry<int>? DifficultyModifier { get; private set; }

        internal void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                key,
                "Patch Enabled",
                true,
                description);

            DetectionRadius = config.Bind(
                key,
                "Detection Radius",
                4,
                "Witness range in tiles; 目击范围");

            if (key == "Steal") {
                return;
            }

            DifficultyModifier = config.Bind(
                key,
                "Difficulty Modifier",
                0,
                "Extra difficulty modifier added/reduced for each witness; 每个目击者检定的额外难度加值/减值");
        }
    }
}