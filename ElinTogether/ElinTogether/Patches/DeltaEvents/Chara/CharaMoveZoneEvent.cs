using System;
using Cwl.Helper.Unity;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(Chara), nameof(Chara.MoveZone), typeof(Zone), typeof(ZoneTransition))]
internal static class CharaMoveZoneEvent
{
    [HarmonyPostfix]
    private static void OnHostMoveZone(Chara __instance, Zone z, ZoneTransition transition)
    {
        // we are not host
        if (NetSession.Instance.Connection is not ElinNetHost { IsConnected: true } host) {
            return;
        }

        // broadcast all map assets to clients early when host finishes map loading
        if (__instance.IsPC) {
            CoroutineHelper.Deferred(() => host.PropagateZoneChangeState(z));
        }
    }

    [HarmonyPrefix]
    private static bool OnClientMoveZone(Chara __instance, Zone z)
    {
        // we are not client
        if (NetSession.Instance.IsHost) {
            return true;
        }

        // remote characters do not trigger scene change
        if (!__instance.IsPC) {
            return true;
        }

        // client side moving initiated from other characters
        // may duplicate client move zone again
        // this happens when we relay host character move zone
        // while clients are in the party - they should move together
        if (__instance.currentZone == z) {
            return false;
        }

        // only proceed if remote zone has been updated
        if (NetSession.Instance.CurrentZone == z) {
            EmpPop.Debug("Host initiated zone state change");
            return true;
        }

        // clients do not post move zone delta
        EmpPop.Debug("Client zone moving is disabled");
        return false;
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_MoveZone3(Zone zone, ZoneTransition transition)
        {
            throw new NotImplementedException("Chara.MoveZone3");
        }
    }
}