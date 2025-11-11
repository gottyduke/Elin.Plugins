using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Attributes;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch]
internal static class CardGenEvent
{
    internal static readonly ConcurrentDictionary<int, Card> HeldRefCards = [];

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(CharaGen), nameof(CharaGen.Create)),
            AccessTools.Method(typeof(ThingGen), nameof(ThingGen._Create)),
        ];
    }

    internal static Card? TryPop(int uid)
    {
        if (!HeldRefCards.TryRemove(uid, out var card)) {
            return null;
        }

        foreach (var staleUid in HeldRefCards.Keys.ToArray()) {
            if (staleUid < uid) {
                HeldRefCards.Remove(staleUid, out _);
            }
        }

        return card;
    }

    [HarmonyPostfix]
    private static void OnCardGenCreate(Card __result)
    {
        // we should relay every single creation call so remotes can hold references
        if (NetSession.Instance.Connection is { IsConnected: true } connection) {
            connection.Delta.AddRemote(new CardGenDelta {
                Card = RemoteCard.Create(__result, true),
            });
        }
    }

    [CwlPostLoad]
    private static void ClearRef()
    {
        HeldRefCards.Clear();
    }
}