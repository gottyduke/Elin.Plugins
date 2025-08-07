using System;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Zones;

[HarmonyPatch]
internal class SafeCreateZonePatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    internal static void ResolveZone(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return;
        }

        if (objectType != typeof(Spatial)) {
            return;
        }

        readType = typeof(Zone);
        resolved = true;
        CwlMod.WarnWithPopup<Zone>("cwl_warn_deserialize".Loc(nameof(Zone), qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EloMap), nameof(EloMap.Init))]
    internal static void OnInitMap(EloMap __instance)
    {
        var list = __instance.region.children;
        list.ForeachReverse(z => {
            if (EMono.sources.zones.map.ContainsKey(z.id)) {
                return;
            }

            list.Remove(z);
            CwlMod.Log<Zone>("cwl_log_post_cleanup".Loc(nameof(Spatial), z.id));
        });
    }

    [SwallowExceptions]
    [CwlSceneInitEvent(Scene.Mode.StartGame, preInit: true)]
    private static void PostCleanup()
    {
        var map = EClass.game.spatials.map;
        foreach (var (id, zone) in map.ToArray()) {
            if (EMono.sources.zones.map.ContainsKey(zone.id)) {
                continue;
            }

            map.Remove(id);
            CwlMod.Log<Zone>("cwl_log_post_cleanup".Loc(nameof(Zone), zone.id));
        }
    }
}