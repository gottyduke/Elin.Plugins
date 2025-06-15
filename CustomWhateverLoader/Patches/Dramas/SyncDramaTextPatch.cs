using System.Collections.Generic;
using Cwl.API.Drama;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class SyncDramaTextPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static void OnParseLineItem(DramaManager __instance, Dictionary<string, string> item)
    {
        //! cookie must be set first to share parse state between patches
        DramaExpansion.Cookie = new(__instance, item);
    }
}