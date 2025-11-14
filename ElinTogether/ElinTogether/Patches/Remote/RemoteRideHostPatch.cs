using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemoteRideHostPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseGameScreen), nameof(BaseGameScreen.RefreshPosition))]
    internal static void OnSetHostRideFocus(BaseGameScreen __instance)
    {
        if (EClass.pc.host is { } host) {
            __instance.focusPos = host.pos.Position();
        }
    }
}