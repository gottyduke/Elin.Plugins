using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaProgressCompleteEvent
{
    internal static List<ElinDelta> DeltaList = [];
    internal static Chara? Chara { get; private set; }
    internal static bool IsHappening { get; private set; }
    internal static AIAct? Action { get; private set; }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer
            .FindAllOverrides(typeof(AIAct), nameof(AIAct.OnProgressComplete))
            .Where(mi => typeof(AIProgress).IsAssignableFrom(mi.DeclaringType) || mi.DeclaringType == typeof(TaskBuild));
    }

    [HarmonyPrefix]
    internal static bool OnProgressComplete(AIAct __instance)
    {
        if (NetSession.Instance.Connection is not { } connection || __instance.owner is null) {
            return true;
        }

        Chara = __instance.owner;
        Action = __instance;
        IsHappening = true;

        if (__instance is not TaskBuild taskBuild) {
            return true;
        }

        if (Chara.IsPC && !ElinDelta.IsApplying) {
            SendCharaBuildDelta(taskBuild);
        }

        return connection.IsHost || ElinDelta.IsApplying;
    }

    [HarmonyPostfix]
    internal static void OnProgressCompleteEnd(AIAct __instance)
    {
        Chara = null;
        Action = null;
        IsHappening = false;

        if (__instance.owner is null) {
            return;
        }

        // only host can complete progress
        if (NetSession.Instance.Connection is not ElinNetHost connection) {
            return;
        }

        if (__instance is TaskBuild) {
            return;
        }

        // due to randomness in max progress
        // remote needs to be notified that a remote task is completed before starting anew
        connection.Delta.AddRemote(new CharaProgressCompleteDelta {
            Owner = __instance.owner,
            CompletedActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
            DeltaList = DeltaList.ToList(),
        });

        DeltaList.Clear();
    }

    internal static void SendCharaBuildDelta(TaskBuild taskBuild)
    {
        if (taskBuild.held is null) {
            return;
        }

        NetSession.Instance.Connection!.Delta.AddRemote(new CharaBuildDelta {
            Held = taskBuild.held,
            Owner = taskBuild.owner,
            Pos = taskBuild.pos,
            Dir = taskBuild.recipe._dir,
            Altitude = taskBuild.altitude,
            BridgeHeight = taskBuild.bridgeHeight,
        });
    }
}