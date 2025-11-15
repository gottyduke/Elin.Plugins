using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.TickConditions))]
internal static class CharaTickConditionEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaTickConditions(Chara __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        // only host and clients can tick conditions
        // host also relays it to all other clients
        if (!connection.IsHost && !__instance.IsPC) {
            return false;
        }

        connection.Delta.AddRemote(new CharaTickConditionDelta {
            Owner = __instance,
        });

        return true;
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_TickConditions()
        {
            throw new NotImplementedException("Chara.TickConditions");
        }
    }
}