using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(InvOwner.Transaction), nameof(InvOwner.Transaction.Process))]
internal static class InvTransactionEvent
{
    [HarmonyPrefix]
    internal static bool OnTransactionProcess(InvOwner.Transaction __instance, bool startTransaction)
    {
        if (NetSession.Instance.Connection is not ElinNetClient client || ElinDelta.IsApplying) {
            return true;
        }

        if (!CardCache.Contains(__instance.thing)) {
            return false;
        }

        if (__instance.thing.parent is null) {
            return true;
        }

        ThingRequest
            .Create(__instance.thing, __instance.num)
            .Then(thing => {
                __instance.thing = thing;
                __instance.Process(startTransaction);
            });

        return false;
    }
}