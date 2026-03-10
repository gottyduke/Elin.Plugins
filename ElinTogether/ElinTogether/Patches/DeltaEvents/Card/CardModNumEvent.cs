using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.ModNum))]
internal static class CardModNumEvent
{
    // [HarmonyPrefix]
    // internal static bool OnCardModNum()
    // {
    //     return !CharaProgressCompleteEvent.IsHappening || NetSession.Instance.IsHost;
    // }

    [HarmonyPostfix]
    internal static void OnCardModNumEnd(Card __instance, int a)
    {
        if (NetSession.Instance.Connection is not { } connection || a == 0) {
            return;
        }

        if (!CardCache.Contains(__instance)) {
            return;
        }

        connection.Delta.AddRemote(new CardModNumDelta {
            Card = __instance,
            Num = __instance.Num,
        });
    }
}