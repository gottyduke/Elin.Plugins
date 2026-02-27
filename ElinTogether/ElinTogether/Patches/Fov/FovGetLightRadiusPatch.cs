using System;
using ElinTogether.Helper;
using HarmonyLib;

namespace ElinTogether.Patches;

// hand of 105gun
[HarmonyPatch(typeof(Card), nameof(Card.GetLightRadius))]
internal class FovGetLightRadiusPatch
{
    [HarmonyPrefix]
    internal static bool OnCardGetLightRadius(Card __instance, ref int __result)
    {
        // treat this card as pc
        if (__instance is not Chara { IsPCOrRemotePlayer: true } chara) {
            return true;
        }

        if (chara.isBlind) {
            __result = 1;
            return false;
        }

        // copied from Card.GetLightRadius
        var baseRadius = EClass._map.IsIndoor || EClass.world.date.IsNight
            ? 2
            : EClass.world.date.periodOfDay == PeriodOfDay.Day
                ? 6
                : 5;

        var extraRadius = 2;
        if (chara.body.GetEquippedThing(SLOT.lightsource)?.trait is TraitLightSource trait) {
            extraRadius = trait.LightRadius;
        }

        var heldLightRadius = chara.held?.GetLightRadius() ?? 0;
        if (heldLightRadius > 0) {
            extraRadius = Math.Max(extraRadius, heldLightRadius - 1);
            extraRadius = Math.Max(extraRadius, 3);
        }

        __result = Math.Max(baseRadius, extraRadius);

        return false;
    }
}