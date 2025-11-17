using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.GetLightRadius))]
internal class FovGetLightRadiusPatch
{
    [HarmonyPrefix]
    internal static bool OnCardGetLightRadius(Card __instance, ref int __result)
    {
        // copyed from Card.GetLightRadius
        // treat this card as pc
        if (FovCalculateFOVPatch.needOverride)
        {
            EmpLog.Verbose($"OnCardGetLightRadius treat {__instance.Name} as MP chara");
            int num = ((__instance.LightData != null) ? __instance.LightData.radius : 0);
            int num2 = 0;
            if (__instance.Chara.isBlind)
            {
                __result = 1;
                return false;
            }
            num = ((EClass._map.IsIndoor || EClass.world.date.IsNight) ? 2 : ((EClass.world.date.periodOfDay == PeriodOfDay.Day) ? 6 : 5));
            num2 = 2;
            Thing equippedThing = __instance.Chara.body.GetEquippedThing(45);
			if (equippedThing != null && equippedThing.trait is TraitLightSource traitLightSource)
			{
				num2 = traitLightSource.LightRadius;
			}
			if (__instance.Chara.held != null)
			{
				int lightRadius = __instance.Chara.held.GetLightRadius();
				if (lightRadius > 0)
				{
					if (lightRadius > num2)
					{
						num2 = __instance.Chara.held.GetLightRadius() - 1;
					}
					if (num2 < 3)
					{
						num2 = 3;
					}
				}
			}
			if (num < num2)
			{
				num = num2;
			}
            __result = num;
            return false;
        }
        return true;
    }
}
