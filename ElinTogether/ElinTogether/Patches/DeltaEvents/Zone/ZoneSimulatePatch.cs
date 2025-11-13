using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class ZoneSimulatePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Zone), nameof(Zone.OnBeforeSimulate));
    }

    [HarmonyPrefix]
    internal static bool OnClientSimulateZone()
    {
        // clients should not simulate anything
        return NetSession.Instance.IsHost;
    }
}