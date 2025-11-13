using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.MoveZone), typeof(Zone), typeof(ZoneTransition))]
internal class CharaMoveZoneEvent
{
    [HarmonyPrefix]
    internal static bool OnClientMoveZone(Chara __instance, Zone z)
    {
        // we are not client
        if (NetSession.Instance.IsHost) {
            return true;
        }

        if (!__instance.IsPC) {
            return true;
        }

        // remote has been updated, okay to proceed
        if (z == NetSession.Instance.CurrentZone) {
            return true;
        }

        // remote characters do not trigger scene change
        // clients do not post move zone delta
        EmpPop.Debug("Client cannot move zone");
        return false;
    }
}