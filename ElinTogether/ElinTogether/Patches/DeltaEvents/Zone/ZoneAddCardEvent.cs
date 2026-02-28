using System;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Zone), nameof(Zone.AddCard), typeof(Card), typeof(int), typeof(int))]
internal static class ZoneAddCardEvent
{
    [HarmonyPrefix]
    internal static bool OnAddCardToZone(Zone __instance, Card t, int x, int z)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        if (!CardCache.Contains(t)) {
            return false;
        }

        // only host can propagate add card event to remotes
        RemoteCard card = t.isThing ? RemoteCard.Create(t, true) : t;
        connection.Delta.AddRemote(new ZoneAddCardDelta {
            Card = card,
            ZoneUid = __instance.uid,
            Pos = new() { X = x, Z = z },
        });

        return true;
    }

    extension(Zone zone)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal Card Stub_AddCard(Card card, int x, int z)
        {
            throw new NotImplementedException("Zone.AddCard");
        }
    }
}