using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Revive))]
internal static class CharaReviveEvent
{
    [HarmonyPrefix]
    internal static void OnCharaRevive(Chara __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        connection.Delta.AddRemote(new CharaReviveDelta {
            Owner = __instance,
        });
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_Revive(Point? p = null, bool msg = false)
        {
            throw new NotImplementedException("Chara.Revive");
        }
    }
}