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

        var frameWidth = __instance.body.sizeDelta.x;
        var frameHeight = __instance.body.sizeDelta.y;

        var maxWidth = Mathf.Max(tileWidth, frameWidth);
        var maxHeight = maxWidth * tileHeight / tileWidth * 32 / 48;

        if (maxHeight < frameHeight) {
            maxWidth *= frameHeight / maxHeight;
            maxHeight = frameHeight;
        }
        
        __instance.body.sizeDelta = new(maxWidth, maxHeight);
    }
}