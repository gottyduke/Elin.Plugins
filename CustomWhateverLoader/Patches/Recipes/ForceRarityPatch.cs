using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Recipes;

[HarmonyPatch]
internal class ForceRarityPatch
{
    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Card), nameof(Card.OnCreate), typeof(int));
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void OnResetRarity(Card __instance)
    {
        var row = __instance.sourceCard;
        if (row.tag.Contains("forceRarity")) {
            __instance.ChangeRarity(row.quality.ToEnum<Rarity>());
        }
    }
}