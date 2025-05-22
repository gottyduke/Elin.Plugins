using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class RepositionTcPatch
{
    private static readonly Dictionary<int, float> _cached = [];

    private static float CacheRaycastDistTwoWay(Sprite sprite, int uid)
    {
        if (_cached.TryGetValue(uid, out var dist)) {
            return dist;
        }

        var distUp = RepositionPortraitPatch.CacheRaycastDist(sprite, uid, false);
        var distDown = RepositionPortraitPatch.CacheRaycastDist(sprite, uid);

        return _cached[uid] = distDown - distUp;
    }

    private static bool IsSpriteReplacerBased(TC tc)
    {
        var renderer = tc.render;
        return renderer is { actor: { isPCC: false } actor, usePass: false } && actor.sr != null;
    }

    [HarmonyPatch]
    internal class TcFixPosPatch
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer.FindAllOverridesGetter(typeof(TC), nameof(TC.FixPos));
        }

        [HarmonyPostfix]
        internal static void OnGetFixPos(TC __instance, ref Vector3 __result)
        {
            if (!IsSpriteReplacerBased(__instance)) {
                return;
            }

            var actor = __instance.render.actor;
            var uid = actor.owner.uid;
            var sprite = actor.sr.sprite;

            var yOffset = CacheRaycastDistTwoWay(sprite, uid) + TC._setting.textPos.y;
            const float wiggleRoom = 2f;

            __result.y -= yOffset - wiggleRoom;
        }
    }

    [HarmonyPatch]
    internal class TcOrbitRefreshPatch
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer.FindAllOverrides(typeof(TCOrbit), nameof(TCOrbit.Refresh));
        }

        [HarmonyPostfix]
        internal static void OnRefreshOrbit(TCOrbit __instance)
        {
            if (!IsSpriteReplacerBased(__instance)) {
                return;
            }

            var pos = __instance.transform.position;
            var data = __instance.render.data;
            __instance.transform.position = pos with { y = pos.y - data.offset.y + data.size.y };
        }
    }
}