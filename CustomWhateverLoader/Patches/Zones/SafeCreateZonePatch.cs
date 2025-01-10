using System;
using System.Linq;
using Cwl.API.Processors;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Zones;

[HarmonyPatch]
internal class SafeCreateZonePatch
{
    private static bool _cleanup;

    internal static bool Prepare()
    {
        if (CwlConfig.SafeCreateClass) {
            TypeResolver.Add(ResolveZones);
        }

        return CwlConfig.SafeCreateClass;
    }

    private static void ResolveZones(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return;
        }

        if (objectType != typeof(Spatial) || readType != typeof(object)) {
            return;
        }

        readType = typeof(Zone);
        resolved = true;
        CwlMod.Warn("cwl_warn_deserialize".Loc(nameof(Zone), qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));

        if (!_cleanup) {
            SafeSceneInitPatch.Cleanups.Enqueue(PostCleanup);
        }

        _cleanup = true;
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EloMap), nameof(EloMap.Init))]
    internal static void OnInitMap(EloMap __instance)
    {
        if (!_cleanup) {
            return;
        }

        var list = __instance.region.children;
        list.ForeachReverse(z => {
            if (EMono.sources.zones.map.ContainsKey(z.id)) {
                return;
            }

            list.Remove(z);
            CwlMod.Log("cwl_log_post_cleanup".Loc(nameof(Spatial), z.id));
        });
    }

    [SwallowExceptions]
    private static void PostCleanup()
    {
        if (!_cleanup) {
            return;
        }

        var map = EClass.game.spatials.map;
        foreach (var (id, zone) in map.ToList()) {
            if (EMono.sources.zones.map.ContainsKey(zone.id)) {
                continue;
            }

            map.Remove(id);
            CwlMod.Log("cwl_log_post_cleanup".Loc(nameof(Zone), zone.id));
        }
    }
}