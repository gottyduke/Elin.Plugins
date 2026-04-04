using System.Collections.Generic;
using System.Reflection;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CardGenEvent
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(CharaGen), nameof(CharaGen.Create)),
            AccessTools.Method(typeof(ThingGen), nameof(ThingGen._Create)),
        ];
    }

    [HarmonyPostfix]
    internal static void OnCardGenCreate(Card __result)
    {
        // we should relay every single creation call so remotes can hold references
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // we use negative uid to avoid conflicting with host
        if (connection.IsClient) {
            __result.uid = -__result.uid;
            return;
        }

        connection.Delta.AddRemote(new CardGenDelta {
            Card = RemoteCard.Create(__result, true),
        });
    }
}