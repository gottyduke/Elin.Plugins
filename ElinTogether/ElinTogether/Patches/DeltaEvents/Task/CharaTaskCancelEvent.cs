using System;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(AIAct), nameof(AIAct.Cancel))]
internal static class CharaTaskCancelEvent
{
    [HarmonyPrefix]
    internal static bool OnCancel(AIAct __instance)
    {
        if (__instance.owner is not { } owner || owner.ai.Current is not AIProgress current) {
            return true;
        }

        var prevent = false;
        var net = NetSession.Instance.Connection;
        switch (net) {
            case ElinNetHost when owner.ai is GoalRemote:
                break;
            case ElinNetClient when owner.IsPC:
                // client can only cancel progress with delta
                prevent = true;
                break;
            default:
                return true;
        }

        net.Delta.AddRemote(new CharaTaskCancelDelta {
            Owner = owner,
            ActId = SourceValidation.ActToIdMapping[current.parent.GetType()],
        });

        return !prevent;
    }

    extension(AIAct aIAct)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal AIAct.Status Stub_Cancel()
        {
            throw new NotImplementedException("AIAct.Cancel");
        }
    }
}