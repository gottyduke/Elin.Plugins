using HarmonyLib;
using UnityEngine;

namespace VSS.Patches;

[HarmonyPatch]
internal class UpdateSpritePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TestActor), nameof(TestActor.UpdateSprite))]
    internal static void OnUpdateSprite(TestActor __instance)
    {
        var tileWidth = __instance.provider.vCurrent.tex.width / 4;
        var tileHeight = __instance.provider.vCurrent.tex.height / 4;

        var maxWidth = Mathf.Max(tileWidth, __instance.body.sizeDelta.x);
        var maxHeight = maxWidth * tileHeight / tileWidth * 32 / 48;
        __instance.body.sizeDelta = new(maxWidth, maxHeight);
    }
}