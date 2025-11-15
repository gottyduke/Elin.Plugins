using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Revive))]
internal static class CharaReviveEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaRevive(Chara __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        // drop all other character revives and wait for delta
        if (!connection.IsHost && !__instance.IsPC) {
            return false;
        }

        connection.Delta.AddRemote(new CharaReviveDelta {
            Owner = __instance,
        });

        return true;
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