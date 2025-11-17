using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class CharaTaskCompleteEvent
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(AIAct), nameof(AIAct.OnProgressComplete));
    }

    [HarmonyPrefix]
    internal static void OnRemoteTaskComplete(AIAct __instance)
    {
        if (__instance.owner is null) {
            return;
        }

        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // drop all other task completes and wait for delta
        if (!connection.IsHost && !__instance.owner.IsPC) {
            return;
        }

        // due to randomness in max progress
        // remote needs to be notified that a remote task is completed before starting anew
        connection.Delta.AddRemote(new CharaTaskDelta {
            Owner = __instance.owner,
            TaskArgs = null,
            Complete = true,
        });
    }
}