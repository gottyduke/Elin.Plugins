using System.Collections.Generic;
using BepInEx;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

internal class ExpandedActionPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.ExpandedActions;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static void OnSwitchAction(DramaManager __instance, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("action", out var action) || action.IsNullOrWhiteSpace()) {
        }
    }
}