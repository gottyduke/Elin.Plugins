using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Unity;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

/// <summary>
///     For spatial gen, we don't necessarily need to send zone instances in packets
///     It is not a random instantiation unlike CardGen
/// </summary>
[HarmonyPatch(typeof(SpatialGen), nameof(SpatialGen.Create))]
internal static class SpatialGenEvent
{
    internal static readonly ConcurrentDictionary<int, Zone> HeldRefZones = [];

    internal static Zone? TryPop(int uid)
    {
        if (!HeldRefZones.TryRemove(uid, out var card)) {
            return null;
        }

        foreach (var staleUid in HeldRefZones.Keys.ToArray()) {
            if (staleUid < uid) {
                HeldRefZones.Remove(staleUid, out _);
            }
        }

        return card;
    }

    [HarmonyPostfix]
    internal static void OnSpatialGen(Spatial __result)
    {
        if (NetSession.Instance.Connection is not ElinNetHost  host) {
            return;
        }

        // host propagates all zone creation for clients to hold references
        // must defer this because dungeon levels are assigned after creation
        CoroutineHelper.Deferred(() => host.Delta.AddRemote(SpatialGenDelta.Create(__result as Zone)));
    }
}