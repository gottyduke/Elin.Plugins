using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Renderers;

//[HarmonyPatch]
internal class GetSpriteOverridePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Card), nameof(Card.GetSprite), typeof(int));
    }

    [HarmonyPostfix]
    internal static void OnGetCardSprite(Card __instance, ref Sprite __result)
    {
        var hasOverride = __instance.mapStr.TryGetValue(CwlReservedConstants.SpriteOverride, out var overrideKey);
        if (hasOverride && TryGetSprite(overrideKey, out var overrideSprite)) {
            __result = overrideSprite;
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