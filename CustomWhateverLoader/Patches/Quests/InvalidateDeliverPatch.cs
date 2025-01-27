using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class InvalidateItemPatch
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

[HarmonyPatch]
internal static class InvalidateDestThingPatch
{
    private static readonly List<Thing> _cleanup = [];
    private static bool _queued;

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(QuestDeliver), nameof(QuestDeliver.IsDestThing), typeof(Thing));
    }

    [HarmonyPrefix]
    internal static bool InvalidateDestThing(QuestDeliver __instance, ref bool __result, Thing t)
    {
        try {
            _ = t.source.GetName();
            _ = t.GetName(NameStyle.Simple, 1);
        } catch (Exception ex) {
            if (!_cleanup.Contains(t)) {
                _cleanup.Add(t);

                if (!_queued) {
                    CoroutineHelper.Deferred(Cleanup);
                    _queued = true;
                }

                CwlMod.Warn<QuestDeliver>("cwl_warn_quest_id_thing2".Loc(ex.Message));
            }

            __result = false;
            return false;
            // noexcept
        }

        return true;
    }

    private static void Cleanup()
    {
        foreach (var thing in _cleanup) {
            try {
                thing?.Destroy();
            } catch {
                // noexcept
            }
        }

        _cleanup.Clear();
        _queued = false;
    }
}