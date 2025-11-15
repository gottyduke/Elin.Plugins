using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.SetPlaceState))]
internal static class CardSetPlacedStateEvent
{
    [HarmonyPrefix]
    internal static void OnSetCardPlacedState(Card __instance, PlaceState newState, bool byPlayer)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        if (newState == PlaceState.none) {
            return;
        }

        // we propagate every place event to remotes
        // so clients can help with placing stuff
        // Elin: Build Together
        connection.Delta.AddRemote(new CardPlacedDelta {
            Owner = __instance,
            PlaceState = newState,
            ByPlayer = byPlayer,
        });
    }

    extension(Card card)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_SetPlacedState(PlaceState newState, bool byPlayer = false)
        {
            throw new NotImplementedException("Card.SetPlacedState");
        }
    }
}