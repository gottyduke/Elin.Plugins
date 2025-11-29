using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Custom;
using Cwl.Helper;
using HarmonyLib;

namespace Cwl.Patches.Traits;

internal class ConverterEvent
{
    internal class CanDecaySubEvent
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer
                .FindAllOverrides(typeof(TraitBrewery), nameof(TraitBrewery.CanChildDecay), typeof(Card))
                .Where(mi => mi.DeclaringType != typeof(CustomConverter));
        }

        [HarmonyPrefix]
        internal static bool OnCheckDecay(TraitBrewery __instance, ref bool __result, MethodInfo __originalMethod, Card __0)
        {
            if (CustomConverter.GetConverter(__instance.owner) is not { } converter) {
                return true;
            }

            if (__originalMethod.DeclaringType == __instance.GetType()) {
                return true;
            }

            converter.owner = __instance.owner;
            __result = converter.CanChildDecay(__0);

            return false;
        }
    }

    internal class OnDecaySubEvent
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer
                .FindAllOverrides(typeof(TraitBrewery), nameof(TraitBrewery.OnChildDecay), typeof(Card), typeof(bool))
                .Where(mi => mi.DeclaringType != typeof(CustomConverter));
        }

        [HarmonyPrefix]
        internal static bool OnConversion(TraitBrewery __instance,
                                          ref bool __result,
                                          MethodInfo __originalMethod,
                                          Card __0,
                                          bool __1)
        {
            if (CustomConverter.GetConverter(__instance.owner) is not { } converter) {
                return true;
            }

            if (__originalMethod.DeclaringType == __instance.GetType()) {
                return true;
            }

            converter.owner = __instance.owner;
            __result = converter.OnChildDecay(__0, __1);

            return false;
        }
    }
}