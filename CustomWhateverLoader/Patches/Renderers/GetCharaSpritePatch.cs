using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Renderers;

[HarmonyPatch]
internal class GetCharaSpritePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.GetSprite))]
    internal static void OnGetCharaSprite(Chara __instance, int dir, ref Sprite __result)
    {
        if (__instance.mapStr.TryGetValue("sprite_override", out var @override) &&
            SpriteReplacer.dictModItems.TryGetValue(@override, out var texture) &&
            texture.LoadSprite() is { } sprite) {
            __result = sprite;
        }
    }
}