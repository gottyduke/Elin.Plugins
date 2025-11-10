using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(Chara), nameof(Chara.Pick))]
internal class CharaPickThingEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaPickThingy(Chara __instance, Thing t)
    {
        var session = NetSession.Instance;
        if (session.Connection is not { IsConnected: true } connection) {
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

        return true;
    }
}