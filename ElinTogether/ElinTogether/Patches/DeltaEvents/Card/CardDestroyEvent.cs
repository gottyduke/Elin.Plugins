using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.Destroy))]
internal static class CardDestroyEvent
{
    [HarmonyPrefix]
    internal static void OnCardModNum(Card __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        if (!CardCache.Contains(__instance)) {
            return;
        }

        // delta will be sent in CardModNumEvent
        if (__instance.Num <= 0) {
            return;
        }

        connection.Delta.AddRemote(new CardModNumDelta {
            Card = __instance,
            Num = 0,
        });
    }
}