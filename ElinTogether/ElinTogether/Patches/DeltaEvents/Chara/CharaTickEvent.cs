using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(Chara), nameof(Chara.Tick))]
internal static class CharaTickEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaTick(Chara __instance)
    {
        if (NetSession.Instance.Connection is not { IsConnected: true } connection) {
            return true;
        }

        // when any player tick, it should tick the host world
        // which should relay to all players again
        if (!connection.IsHost && !__instance.IsPC) {
            return false;
        }

        connection.Delta.AddRemote(new CharaTickDelta {
            Owner = __instance,
        });

        return true;
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_Tick()
        {
            throw new NotImplementedException("Chara.Tick");
        }
    }
}