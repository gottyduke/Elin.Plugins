using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara._Move))]
internal static class CharaMoveEvent
{
    [HarmonyPrefix]
    internal static void OnCharaMove(Chara __instance, Point newPoint, ref Card.MoveType type)
    {
        var session = NetSession.Instance;
        if (session.Connection is not { } connection) {
            return;
        }

        // we are host, everyone should generate a delta
        // also relay the client move to other clients

        // we are client, only ourselves should generate a delta
        // ignore all other relayed moves

        if (!connection.IsHost && !__instance.IsPC) {
            return;
        }

        // movement could be random in certain circumstances
        // not really a big problem if everyone is moving
        // simply ignore and wait for reconciliation or next delta

        // and let's not push each other because that is so random
        if (__instance.IsPC) {
            type = Card.MoveType.Force;
        }

        connection.Delta.AddRemote(new CharaMoveDelta {
            Owner = __instance,
            Pos = newPoint,
            MoveType = (CharaMoveDelta.CharaMoveType)Card.MoveType.Force,
        });
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal Card.MoveResult Stub_Move(Point point, Card.MoveType type)
        {
            throw new NotImplementedException("Chara.Move");
        }
    }
}