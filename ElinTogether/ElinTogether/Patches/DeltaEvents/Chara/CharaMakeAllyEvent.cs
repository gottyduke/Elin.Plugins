using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(Chara), nameof(Chara.MakeAlly))]
internal static class CharaMakeAllyEvent
{
    [HarmonyPrefix]
    internal static bool OnMakeAlly(Chara __instance, bool msg)
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost { IsConnected: true } host:
                host.Delta.AddRemote(new CharaMakeAllyDelta {
                    Owner = __instance,
                    ShowMsg = msg,
                });
                return true;
            case ElinNetClient { IsConnected: true }:
                // we are clients, drop the update and wait for delta
                return false;
            default:
                return true;
        }
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_MakeAlly(bool msg)
        {
            throw new NotImplementedException("Chara.MakeAlly");
        }
    }
}