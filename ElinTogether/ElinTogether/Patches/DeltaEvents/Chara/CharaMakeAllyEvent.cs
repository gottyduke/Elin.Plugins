using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.MakeAlly))]
internal static class CharaMakeAllyEvent
{
    [HarmonyPrefix]
    internal static bool OnMakeAlly(Chara __instance, bool msg)
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost  host:
                host.Delta.AddRemote(new CharaMakeAllyDelta {
                    Owner = __instance,
                    ShowMsg = msg,
                    TemporaryAllyName = __instance.c_altName,
                });
                return true;
            case ElinNetClient :
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