using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Custom;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Traits;

[HarmonyPatch]
internal class ConverterEvent
{
    [HarmonyPatch]
    internal class CanDecaySubEvent
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer
                .FindAllOverrides(typeof(TraitBrewery), nameof(TraitBrewery.CanChildDecay), typeof(Card))
                .Where(mi => mi.DeclaringType != typeof(CustomConverter));
        }

        [HarmonyPrefix]
        internal static bool OnCheckDecay(TraitBrewery __instance, ref bool __result, MethodInfo __originalMethod, Card c)
        {
            if (CustomConverter.GetConverter(__instance.owner) is not { } converter) {
                return true;
            }

            if (__originalMethod.DeclaringType == __instance.GetType()) {
                return true;
            }

            converter.owner = __instance.owner;
            __result = converter.CanChildDecay(c);

            return false;
        }
    }

    [HarmonyPatch]
    internal class OnDecaySubEvent
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer
                .FindAllOverrides(typeof(TraitBrewery), nameof(TraitBrewery.OnChildDecay), typeof(Card), typeof(bool))
                .Where(mi => mi.DeclaringType != typeof(CustomConverter));
        }

        [HarmonyPrefix]
        internal static bool OnConversion(TraitBrewery __instance, ref bool __result, MethodInfo __originalMethod, Card c,
            bool firstDecay)
        {
            if (CustomConverter.GetConverter(__instance.owner) is not { } converter) {
                return true;
            }

            if (__originalMethod.DeclaringType == __instance.GetType()) {
                return true;
            }

            converter.owner = __instance.owner;
            __result = converter.OnChildDecay(c, firstDecay);

            return false;
        }
    }
}