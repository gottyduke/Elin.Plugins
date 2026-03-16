using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Renderers;

[HarmonyPatch]
internal class GetSpriteOverridePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Card), nameof(Card.GetSprite), typeof(int));
    }

    // TODO: maybe support dir? but I really want to avoid runtime features
    [HarmonyPostfix]
    internal static void OnGetCardSprite(Card __instance, int dir, ref Sprite __result)
    {
        var hasOverride = __instance.mapStr.TryGetValue("sprite_override", out var overrideKey);
        var isSnow = EClass._zone?.IsSnowCovered is true;

        if (hasOverride) {
            if (isSnow && TryGetSprite($"{overrideKey}_snow", out var snowOverrideSprite)) {
                __result = snowOverrideSprite;
            } else if (TryGetSprite(overrideKey, out var overrideSprite)) {
                __result = overrideSprite;
            }
        } else {
            if (isSnow && TryGetSprite($"{__instance.id}_snow", out var snowSprite)) {
                __result = snowSprite;
            }
        }
    }

    private static bool TryGetSprite(string key, [NotNullWhen(true)] out Sprite? sprite)
    {
        sprite = null;
        return SpriteReplacer.dictModItems.TryGetValue(key, out var tex) &&
               tex.LoadSprite() is { } loaded &&
               (sprite = loaded) != null;
    }
}