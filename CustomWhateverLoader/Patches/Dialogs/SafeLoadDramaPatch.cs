using System.Collections.Generic;
using Cwl.API.Drama;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class SafeLoadDramaPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static void OnParseLineItem(DramaManager __instance, Dictionary<string, string> item)
    {
        //! cookie must be set first to share parse state between patches
        DramaExpansion.Cookie = new(__instance, item);

        // makes drama writer's life easier
        SyncTexts(item);
    }

    private static void SyncTexts(Dictionary<string, string> item)
    {
        if (!item.TryGetValue("id", out var id) || id.IsEmpty()) {
            return;
        }

        var textLocalize = item["text"];
        var textEn = item["text_EN"];
        var textJp = item["text_JP"];

        if (textJp.IsEmpty()) {
            item["text_JP"] = textEn.IsEmpty(textLocalize.IsEmpty("<empty>"));
        }
    }
}