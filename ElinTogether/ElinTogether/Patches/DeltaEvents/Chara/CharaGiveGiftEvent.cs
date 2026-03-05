using System;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.GiveGift))]
internal static class CharaGiveGiftEvent
{
    [HarmonyPrefix]
    internal static void OnGiveGift(Chara __instance, Chara c, Thing t)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        if (!CardCache.Contains(t)) {
            return;
        }

        connection.Delta.AddRemote(new CharaGiveGiftDelta {
            From = __instance,
            To = c,
            Thing = RemoteCard.Create(t),
        });
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_GiveGift(Chara c, Thing t)
        {
            throw new NotImplementedException("Chara.GiveGift");
        }
    }
}