using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
internal static class ZoneActivateEvent
{
    internal static bool IsActivating { get; private set; }

    [HarmonyPrefix]
    internal static void OnActivateZone(Zone __instance)
    {
        IsActivating = true;
        ActionModeCombat.EnemyVisibility.Clear();
    }

    [HarmonyPostfix]
    internal static void OnActivateZoneEnd(Zone __instance)
    {
        IsActivating = false;
        if (NetSession.Instance.Connection is not null) {
            CardCache.CacheCurrentZone();
        }

        // we are not host
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        // every zone activate should be relayed to clients
        // broadcast all map assets to clients when host finishes map loading
        CoroutineHelper.Deferred(() => host.PropagateZoneChangeState(__instance), 2);
    }
}