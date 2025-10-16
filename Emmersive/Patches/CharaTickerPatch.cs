using Emmersive.Components;
using Emmersive.Helper;
using HarmonyLib;

namespace Emmersive.Patches;

[HarmonyPatch]
internal class CharaTickerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.Tick))]
    internal static void OnPlayerTick(Chara __instance)
    {
        if (!__instance.IsPC) {
            return;
        }

        if (__instance.Profile.TalkOnCooldown || __instance.Profile.LockedInRequest) {
            return;
        }

        var diff = __instance.turn - __instance.Profile.LastReactionTurn;
        if (diff >= EmConfig.Scene.TurnsCooldown.Value * 3) {
            EmScheduler.RequestScenePlayWithTrigger();
        }
    }
}