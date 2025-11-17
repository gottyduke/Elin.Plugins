using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Helper;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class CharaTaskProgressEvent
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(AIAct), nameof(AIAct.CreateProgress));
    }

    [HarmonyPostfix]
    internal static void OnProgress(AIAct __instance, AIProgress __result)
    {
        if (__instance.owner is not { } owner) {
            return;
        }

        switch (NetSession.Instance.Connection) {
            case ElinNetHost host when host.ActiveRemoteCharas.Values.Contains(owner):
            case ElinNetClient when !owner.IsPC:
                // we can only complete remote progress with delta
                __result.progress = int.MaxValue;
                owner.NetProfile.CurrentTask = new(__result, false);
                return;
        }
    }
}