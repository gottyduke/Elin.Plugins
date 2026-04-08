using HarmonyLib;
using UnityEngine;

namespace ACS.Patches;

[HarmonyPatch]
internal class LoadSpritePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpriteData), nameof(SpriteData.Load))]
    internal static void OnAfterFrameCreation(SpriteData __instance)
    {
        if (__instance.frame <= 1) {
            return;
        }

        if (!__instance.id.StartsWith("_acs_")) {
            return;
        }

        var refSprite = __instance.sprites.TryGet(0, true);
        if (refSprite == null) {
            return;
        }

        var width = refSprite.rect.width / __instance.frame;
        var height = refSprite.rect.height;

        for (var i = 0; i < __instance.frame; ++i) {
            var rect = new Rect(i * width, 0f, width, height);
            var pivot = new Vector2(0.5f, 0.5f * (128f / height));
            var sprite = Sprite.Create(__instance.tex, rect, pivot, 100f, 0u, SpriteMeshType.FullRect);
            sprite.name = $"{__instance.id}{i:D4}";
        }
    }
}