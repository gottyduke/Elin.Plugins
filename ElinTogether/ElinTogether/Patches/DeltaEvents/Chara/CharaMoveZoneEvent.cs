using System;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(Chara), nameof(Chara.MoveZone), typeof(Zone), typeof(ZoneTransition))]
internal static class CharaMoveZoneEvent
{
    [HarmonyPostfix]
    internal static void OnHostMoveZone(Chara __instance, Zone z)
    {
        // we are not host
        if (NetSession.Instance.Connection is not ElinNetHost { IsConnected: true } host) {
            return;
        }

        // wait for actual zone activate
        CoroutineHelper.Deferred(() => {
            // broadcast all map assets to clients when host finishes map loading
            if (__instance.IsPC) {
                host.PropagateZoneChangeState(z);
            }

            // every move zone should be relayed to clients
            // when client receives host move zone - which is party leader
            // they will be brought together as party members
            host.Delta.AddRemote(new CharaMoveZoneDelta {
                Owner = __instance,
                ZoneFullName = z.ZoneFullName,
                ZoneUid = z.uid,
                PosX = __instance.pos.x,
                PosZ = __instance.pos.z,
            });
        });
    }

    [HarmonyPrefix]
    internal static bool OnClientMoveZone(Chara __instance, Zone z)
    {
        // we are not client
        if (NetSession.Instance.IsHost) {
            return true;
        }

        // remote characters do not trigger scene change
        // clients do not post move zone delta

        // client side moving initiated from other characters
        // may duplicate client move zone again
        // this happens when we relay host character move zone
        // while clients are in the party - they should move together
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