using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaProgressCompleteEvent
{
    internal static List<ElinDeltaBase> DeltaList = [];
    internal static Chara? Chara { get; private set; }
    internal static bool IsHappening { get; private set; }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer
            .FindAllOverrides(typeof(AIAct), nameof(AIAct.OnProgressComplete))
            .Where(mi => typeof(AIProgress).IsAssignableFrom(mi.DeclaringType));
    }

    [HarmonyPrefix]
    internal static void OnProgressComplete(AIAct __instance)
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost when __instance.owner?.IsPCOrRemotePlayer is true:
            case ElinNetClient when __instance.owner is not null:
                break;
            default:
                return;
        }

        Chara = __instance.owner;
        IsHappening = true;
    }

    [HarmonyPostfix]
    internal static void OnProgressCompleteEnd(AIAct __instance)
    {
        if (__instance.owner is null) {
            return;
        }

        Chara = null;
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
            DeltaList = [..DeltaList],
        });

        DeltaList.Clear();
    }
}