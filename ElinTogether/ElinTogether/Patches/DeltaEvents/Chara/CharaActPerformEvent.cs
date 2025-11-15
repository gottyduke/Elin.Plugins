using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaActPerformEvent
{
    private static readonly HashSet<int> _alwaysSuccessfulActs = [
        // for some reason this returns false
        ABILITY.ActRide,
    ];

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Act), nameof(Act.Perform))
            .Where(mi => mi.DeclaringType != typeof(DynamicAct) && mi.DeclaringType != typeof(DynamicAIAct));
    }

    [HarmonyPostfix]
    internal static void OnCharaActPerform(Act __instance, bool __result)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // to save bandwidth, only propagate successful act perform events
        if (!__result && !_alwaysSuccessfulActs.Contains(__instance.id)) {
            return;
        }

        // host propagates every act perform event
        // clients only propagate self
        if (connection.IsHost || Act.CC.IsPC) {
            connection.Delta.AddRemote(CharaActPerformDelta.Create(__instance));
        }
    }
}