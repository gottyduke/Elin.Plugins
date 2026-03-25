using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.ModNum))]
internal static class CardModNumEvent
{
    [HarmonyPrefix]
    internal static bool OnCardModNum(Card __instance, ref int a)
    {
        // return true if !CardCache.Contains(__instance) -> allow to mod the num of temp items
        if (NetSession.Instance.IsHost || CardModNumDelta.IsApplying || !CardCache.Contains(__instance)) {
            return true;
        }

        a = 0;
        return false;
    }

    [HarmonyPostfix]
    internal static void OnCardModNumEnd(Card __instance, int a)
    {
        if (NetSession.Instance.Connection is not { } connection || a == 0 || CardModNumDelta.IsApplying) {
            return;
        }

        if (!CardCache.Contains(__instance)) {
            return;
        }

        var delta = new CardModNumDelta {
            Card = __instance,
            Num = __instance.Num,
        };

        if (CharaProgressCompleteEvent.IsHappening && NetSession.Instance.IsHost) {
            CharaProgressCompleteEvent.DeltaList.Add(delta);
            return;
        }

        connection.Delta.AddRemote(delta);
    }
}