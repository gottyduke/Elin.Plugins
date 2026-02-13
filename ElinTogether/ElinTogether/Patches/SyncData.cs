using System.Diagnostics;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Core), nameof(Core.Update))]
internal static class SyncData
{
    [HarmonyPrefix]
    internal static void OnCoreUpdate()
    {
        // apply remote delta happened in last update before this update
        switch (NetSession.Instance.Connection) {
            case ElinNetHost host:
                host.WorldStateDeltaProcess();
                return;
            case ElinNetClient client:
                client.WorldStateDeltaProcess();
                return;
        }
    }

    [HarmonyPostfix]
    internal static void OnCoreUpdateEnd()
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost host:
                host.WorldStateDeltaUpdate();
                return;
            case ElinNetClient client:
                client.WorldStateDeltaUpdate();
                return;
        }
    }
}