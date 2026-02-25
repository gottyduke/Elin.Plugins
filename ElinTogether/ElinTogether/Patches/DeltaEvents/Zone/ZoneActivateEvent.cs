using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class ZoneActivateEvent
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
    internal static void OnHostActivateZone(Zone __instance)
    {
        if (NetSession.Instance.Connection is not null) {
            CardCache.CacheCurrentZone();
        }

        // we are not host
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        // every zone activate should be relayed to clients
        // broadcast all map assets to clients when host finishes map loading
        host.PropagateZoneChangeState(__instance);
    }
}