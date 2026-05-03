using System.Collections.Generic;
using System.Reflection;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class InvOwnerOnProcessEvent
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(InvOwnerDraglet), nameof(InvOwnerDraglet.OnProcess)),
            AccessTools.Method(typeof(InvOwnerHotbar), nameof(InvOwnerHotbar.OnProcess)),
        ];
    }

    [HarmonyPrefix]
    internal static void OnProcess(InvOwner __instance, Thing t)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        if (!CardCache.Contains(t)) {
            return;
        }

        if (t.parent is not Card parent) {
            return;
        }

        connection.Delta.AddRemote(new InvOwnerOnProcessDelta {
            Parent = parent,
            Thing = t,
            Dest = __instance.owner,
        });
    }
}