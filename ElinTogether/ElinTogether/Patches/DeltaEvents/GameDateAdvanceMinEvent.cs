using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(GameDate), nameof(GameDate.AdvanceMin))]
internal static class GameDateAdvanceMinEvent
{
    [HarmonyPrefix]
    internal static bool OnGameAdvanceMin(int a)
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost { IsConnected: true } host:
                host.Delta.AddRemote(new GameTimeDelta {
                    AdvanceMin = a,
                });
                return true;
            case ElinNetClient { IsConnected: true }:
                // we are clients, drop the update and wait for delta
                return false;
            default:
                return true;
        }
    }

    extension(GameDate date)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_AdvanceMin(int advance)
        {
            throw new NotImplementedException("GameDate.AdvanceMin");
        }
    }
}