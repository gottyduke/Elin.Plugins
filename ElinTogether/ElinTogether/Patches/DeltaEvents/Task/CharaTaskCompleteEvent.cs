using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaTaskCompleteEvent
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(AIAct), nameof(AIAct.OnProgressComplete));
    }

    [HarmonyPrefix]
    internal static void OnRemoteTaskComplete(AIAct __instance)
    {
        if (__instance is not AIProgress || __instance.owner is null) {
            return;
        }

        // only host can complete progress
        if (NetSession.Instance.Connection is not ElinNetHost connection) {
            return;
        }

        // due to randomness in max progress
        // remote needs to be notified that a remote task is completed before starting anew
        connection.Delta.AddRemote(new CharaTaskDelta {
            Owner = __instance.owner,
            TaskArgs = null,
            CompletedActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
        });
    }
}