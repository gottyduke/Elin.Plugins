using System;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Tick))]
internal static class CharaTickEvent
{
    internal static int LastTick { get; private set; }

    [HarmonyPrefix]
    internal static bool OnCharaTick(Chara __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
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

        LastTick = NetSession.Instance.Tick;

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