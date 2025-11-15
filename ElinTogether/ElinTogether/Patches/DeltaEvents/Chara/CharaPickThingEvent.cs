using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Pick))]
internal static class CharaPickThingEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaPickThingy(Chara __instance, Thing t)
    {
        var session = NetSession.Instance;
        if (session.Connection is not { } connection) {
            return true;
        }

        // we are host, propagate to everyone
        // we are client, only propagate ourselves
        if (connection.IsHost || __instance.IsPC) {
            connection.Delta.AddRemote(new CharaPickThingDelta {
                Owner = __instance,
                Thing = t,
            });
        }

        return connection.IsHost;
    }

    extension(Chara chara)
    {
        internal Thing Stub_Pick(Thing thing, bool msg = true, bool tryStack = true)
        {
            throw new NotImplementedException("Chara.Pick");
        }
    }
}