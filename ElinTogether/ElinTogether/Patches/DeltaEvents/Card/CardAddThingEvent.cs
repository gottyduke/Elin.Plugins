using System;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.AddThing), [typeof(Thing), typeof(bool), typeof(int), typeof(int)])]
internal static class CardAddThingEvent
{
    [HarmonyPrefix]
    internal static void OnCardAddThing(Card __instance, Thing t, bool tryStack, int destInvX, int destInvY)
    {
        if (NetSession.Instance.Connection is not {} connection) {
            return;
        }

        if (!CardCache.Contains(__instance)) {
            return;
        }

        connection.Delta.AddRemote(new CardAddThingDelta {
            Thing = RemoteCard.Create(t),
            Parent = RemoteCard.Create(__instance),
            TryStack = tryStack,
            DestInvX = destInvX,
            DestInvY = destInvY,
        });
    }

    extension(Card card)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal Thing Stub_AddThing(Thing thing, bool tryStack, int destInvX, int destInvY)
        {
            throw new NotImplementedException("Card.AddThing");
        }
    }
}