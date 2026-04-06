using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(UI), nameof(UI.StartDrag))]
internal static class InvStartDragEvent
{
    [HarmonyPrefix]
    internal static bool OnStartDrag(UI __instance, DragItem item)
    {
        if (NetSession.Instance.Connection is not { } connection
            || ElinDelta.IsApplying
            || item is not DragItemCard dragItemCard) {
            return true;
        }

        var thing = dragItemCard.from.thing;
        if (connection.IsHost) {
            connection.Delta.AddRemote(new CardRemoveThingDelta {
                Thing = thing,
            });

            return true;
        }

        if (!CardCache.Contains(thing)) {
            return false;
        }

        if (thing.parent is null && __instance.nextDrag != item) {
            return true;
        }

        ThingRequest
            .Create(thing, thing.Num)
            .Send()
            .Then(thing => {
                dragItemCard.from.thing = thing;
                __instance.StartDrag(dragItemCard);
            });

        return false;
    }
}