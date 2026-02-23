using ElinTogether.Helper;
using HarmonyLib;

namespace ElinTogether.Patches;

// hand of 105gun
[HarmonyPatch(typeof(Card), nameof(Card.CalculateFOV))]
internal class FovCalculateFOVPatch
{
    [HarmonyPrefix]
    internal static void OnCardCalculateFOV(Card __instance, out bool __state)
    {
        __state = false;

        if (__instance is not Chara { IsRemotePlayer: true } chara) {
            return;
        }

        chara.fov ??= chara.CreateFov();
        chara.fov.isPC = true;

        __state = true;
    }

    [HarmonyPostfix]
    internal static void OnCardCalculateFOVPost(Card __instance, bool __state)
    {
        if (__state) {
            __instance.fov?.isPC = false;
        }
    }
}