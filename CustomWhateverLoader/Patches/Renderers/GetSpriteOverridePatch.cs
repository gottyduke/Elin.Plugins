using System.Collections.Generic;
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
        if (__instance.mapStr.TryGetValue("sprite_override", out var @override) &&
            SpriteReplacer.dictModItems.TryGetValue(@override, out var texture) &&
            texture.LoadSprite() is { } sprite) {
            __result = sprite;
        }
    }
}