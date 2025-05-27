using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class RepositionTcPatch
{
    private static float CacheRaycastDistTwoWay(Sprite sprite)
    {
        var distUp = RepositionPortraitPatch.CacheRaycastDist(sprite, false);
        var distDown = RepositionPortraitPatch.CacheRaycastDist(sprite);

        return distDown - distUp;
    }

    private static bool IsSpriteReplacerBased(TC tc)
    {
        var renderer = tc.render;
        return renderer is { actor.isPCC: false, usePass: false };
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

            if (!CwlConfig.FixBaseGamePopup && !__instance.owner.sourceCard.idRenderData.StartsWith("@")) {
                return;
            }

            var actor = __instance.render.actor;
            var sprite = actor.sr.sprite;

            var yOffset = CacheRaycastDistTwoWay(sprite) + TC._setting.textPos.y;

            var data = __instance.render.data;
            var scaler = data.size.y / 0.64f;

            __result.y -= yOffset * scaler;
        }
    }

    [HarmonyPatch]
    internal class TcOrbitRefreshPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TCOrbitChara), nameof(TCOrbitChara.OnSetOwner))]
        internal static void OnInstantiateOrbit(TCOrbitChara __instance)
        {
            if (!IsSpriteReplacerBased(__instance)) {
                return;
            }

            if (!CwlConfig.FixBaseGamePopup && !__instance.owner.sourceCard.idRenderData.StartsWith("@")) {
                return;
            }

            var actor = __instance.render.actor;
            var sprite = actor.sr.sprite;

            var yOffset = CacheRaycastDistTwoWay(sprite) / 100f;

            var data = __instance.render.data;
            var scaler = data.size.y / 0.64f;

            var pos = __instance.goIcon.transform.localPosition;
            __instance.goIcon.transform.localPosition = pos with { y = pos.y - yOffset * scaler };
        }
    }
}