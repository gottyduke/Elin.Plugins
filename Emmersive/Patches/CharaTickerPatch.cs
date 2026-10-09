using System;
using Emmersive.Components;
using Emmersive.Contexts;
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
        if (!__instance.IsPC || !EmScheduler.CanMakeRequest) {
            return;
        }

        var idle = EmConfig.Scene.TurnsIdleTrigger.Value;
        if (idle < 0) {
            return;
        }

        if (__instance.isDead || __instance.conSleep is not null || EClass.ui.TopLayer != null) {
            return;
        }

        if (__instance.Profile is not { OnTalkCooldown: false, LockedInRequest: false } profile) {
            return;
        }

        if (__instance.turn - profile.LastReactionTurn < idle) {
            return;
        }

        profile.ResetTalkCooldown();

        if (NearbyCharaContext.GetNearbyChara(__instance).Count == 0) {
            return;
        }

        try {
            EmScheduler.RequestScenePlayWithTrigger([]);
        } catch (Exception ex) {
            EmMod.Warn<CharaTickerPatch>($"idle trigger failed: {ex}");
        }
    }
}