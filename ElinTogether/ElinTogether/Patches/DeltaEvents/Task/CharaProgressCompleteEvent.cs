using System.Collections.Generic;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.OnProgressComplete))]
internal static class CharaProgressCompleteEvent
{
    internal static Dictionary<Thing, CharaPickThingDelta> Actions = [];
    internal static bool IsHappening { get; private set; }

    [HarmonyPrefix]
    internal static void OnProgressComplete(AIAct __instance)
    {
        if (__instance.owner is null || NetSession.Instance.Connection is null) {
            return;
        }

        IsHappening = true;
    }

    [HarmonyPostfix]
    internal static void OnProgressCompleteEnd(AIAct __instance)
    {
        if (__instance.owner is null) {
            return;
        }

        IsHappening = false;

        // only host can complete progress
        if (NetSession.Instance.Connection is not ElinNetHost connection) {
            return;
        }

        // due to randomness in max progress
        // remote needs to be notified that a remote task is completed before starting anew
        connection.Delta.AddRemote(new CharaProgressCompleteDelta {
            Owner = __instance.owner,
            CompletedActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
            Actions = [..Actions.Values],
        });
    }
}