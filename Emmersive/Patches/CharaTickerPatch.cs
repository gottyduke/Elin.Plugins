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

        var idle = EmConfig.Scene.TurnsIdleTrigger.Value;
        if (idle < 0) {
            return;
        }

        if (__instance.Profile is not { OnTalkCooldown: false, LockedInRequest: false } pc) {
            return;
        }

        var diff = __instance.turn - pc.LastReactionTurn;
        if (diff < idle) {
            return;
        }

        // start global cooldown
        pc.ResetTalkCooldown();

        EmScheduler.RequestScenePlayImmediate();
    }
}