using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Zone), nameof(Zone.AddCard), typeof(Card), typeof(int), typeof(int))]
internal static class ZoneAddCardEvent
{
    [HarmonyPrefix]
    internal static void OnAddCardToZone(Zone __instance, Card t, int x, int z)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // we propagate every add card event to remotes
        // and validate it accordingly by net type
        connection.Delta.AddRemote(new ZoneAddCardDelta {
            Card = t,
            ZoneUid = __instance.uid,
            Pos = new() { X = x, Z = z },
        });
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