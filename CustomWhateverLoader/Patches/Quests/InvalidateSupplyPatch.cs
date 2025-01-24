using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class InvalidateSupplyPatch
{
    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(QuestDeliver), nameof(QuestDeliver.ListDestThing), typeof(bool));
    }

    [HarmonyPrefix]
    internal static bool InvalidateItems(QuestDeliver __instance, ref List<Thing> __result)
    {
        if (__instance.idThing is null) {
            return true;
        }

        if (EMono.sources.things.map.ContainsKey(__instance.idThing)) {
            return true;
        }

        __result = [];
        CwlMod.Warn<InvalidateSupplyPatch>($"quest {__instance.GetType().Name} has invalid item id: {__instance.idThing}\n" +
                                           $"CWL caught the exception and kept the game going");

        return false;
    }
}