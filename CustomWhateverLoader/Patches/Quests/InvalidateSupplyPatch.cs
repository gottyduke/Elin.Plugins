using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class InvalidateSupplyPatch
{
    private const string FallbackItem = "generator_snowman";

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverridesGetter(typeof(QuestDeliver), nameof(QuestDeliver.sourceThing));
    }

    [HarmonyPrefix]
    internal static bool InvalidateItem(QuestDeliver __instance, ref SourceThing.Row __result)
    {
        __instance.idThing = __instance.idThing.IsEmpty(FallbackItem);
        if (EMono.sources.things.map.TryGetValue(__instance.idThing, out __result) && __result is not null) {
            return false;
        }

        CwlMod.Warn<QuestDeliver>("cwl_warn_quest_id_thing".Loc(__instance.GetType().Name, __instance.idThing, FallbackItem));
        __instance.idThing = FallbackItem;

        return true;
    }
}