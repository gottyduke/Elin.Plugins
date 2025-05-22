using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class RepositionBubblePatch
{
    private static readonly Dictionary<int, float> _cached = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardRenderer), nameof(CardRenderer.Say))]
    internal static void OnPopTcText(CardRenderer __instance)
    {
        if (!__instance.hasActor || __instance.usePass || __instance.actor.sr == null) {
            return;
        }

        var uid = __instance.owner.uid;
        if (_cached.ContainsKey(uid)) {
            return;
        }

        var sprite = __instance.actor.sr.sprite;
        var distUp = RepositionPortraitPatch.CacheRaycastDist(sprite, uid, false);
        var distDown = RepositionPortraitPatch.CacheRaycastDist(sprite, uid, true);

        _cached[uid] = distDown - distUp;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TCText), nameof(TCText.FixPos), MethodType.Getter)]
    internal static void OnGetFixPos(TCText __instance, ref Vector3 __result)
    {
        if (!_cached.TryGetValue(__instance.render.owner.uid, out var dist)) {
            return;
        }

        var yOffset = dist + TC._setting.textPos.y;
        const float wiggleRoom = 2f;

        __result.y -= yOffset - wiggleRoom;
    }
}