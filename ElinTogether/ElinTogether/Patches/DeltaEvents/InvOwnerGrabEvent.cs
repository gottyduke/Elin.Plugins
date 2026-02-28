using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(InvOwner), nameof(InvOwner.Grab))]
internal static class InvOwnerGrabEvent
{
    [HarmonyPrefix]
    internal static void OnInvOwnerGrab(DragItemCard.DragInfo from)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        if (!CardCache.Contains(from.thing)) {
            return;
        }

        connection.Delta.AddRemote(new CardRemoveThingDelta {
            Thing = from.thing,
        });
    }
}