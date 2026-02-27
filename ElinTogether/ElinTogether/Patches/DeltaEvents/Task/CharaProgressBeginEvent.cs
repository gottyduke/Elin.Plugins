using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaTaskProgressEvents
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer
            .FindAllOverrides(typeof(AIProgress), nameof(AIProgress.OnProgressBegin))
            .Where(mi => typeof(AIProgress).IsAssignableFrom(mi.DeclaringType));
    }

    [HarmonyPrefix]
    internal static void OnProgressBegin(AIProgress __instance)
    {
        if (__instance.owner is not { } owner) {
            return;
        }

        switch (NetSession.Instance.Connection) {
            case ElinNetHost:
                break;
            case ElinNetClient:
                // we can only complete remote progress with delta
                __instance.progress = -int.MaxValue;
                break;
            default:
                return;
        }

        if (owner.ai is GoalRemote) {
            // for host, run it only when remote players run it
            __instance.progress = -int.MaxValue;
            return;
        }

        NetSession.Instance.Connection.Delta.AddRemote(new CharaProgressBeginDelta {
            Owner = owner,
            Pos = owner.pos,
            MaxProgress = __instance.MaxProgress,
            ActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
        });
    }
}