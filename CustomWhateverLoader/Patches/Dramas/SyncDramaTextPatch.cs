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

        // makes drama writer's life easier
        SyncTexts(item);
    }

    private static void SyncTexts(Dictionary<string, string> item)
    {
        var id = item["id"];
        if (id.IsEmpty()) {
            return;
        }

        item.TryAdd("text", "");
        item.TryAdd("text_EN", "");
        item.TryAdd("text_JP", "");

        if (item.TryGetValue($"text_{Lang.langCode}", out var textLang)) {
            DramaExpansion.Cookie!.Dm.dictLocalize[id] = item["text"] = textLang;
        }

        var textLocalize = item["text"];
        var textEn = item["text_EN"];
        var textJp = item["text_JP"];

        if (textEn.IsEmpty()) {
            item["text_EN"] = textLocalize.IsEmpty(textJp.IsEmpty("<empty>"));
        }

        if (textJp.IsEmpty()) {
            item["text_JP"] = textLocalize.IsEmpty(textEn.IsEmpty("<empty>"));
        }
    }
}