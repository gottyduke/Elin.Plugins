using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.TryStackTo))]
internal static class CardTryStackEvent
{
    [HarmonyPrefix]
    internal static bool OnCardTryStackTo(Card __instance, Thing to, ref bool __result)
    {
        if (NetSession.Instance.Connection is not ElinNetClient client) {
            return true;
        }

        if (!CardCache.Contains(to) != !CardCache.Contains(__instance)) {
            return false;
        }

        if (!__instance.CanStackTo(to)) {
            return false;
        }

        __result = true;
        client.Delta.AddRemote(new CardTryStackToDelta {
            Card = __instance,
            To = to,
            Parent = to.parent as Card,
        });

        return false;
    }
}