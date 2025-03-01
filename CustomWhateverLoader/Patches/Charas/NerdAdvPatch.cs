using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class NerdAdvPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GlobalGoalAdv), nameof(GlobalGoalAdv.OnAdvanceHour))]
    internal static bool OnNerdShouldTouchGrass(GlobalGoalAdv __instance)
    {
        return __instance.owner.GetFlagValue("StayHomeZone") == 0;
    }
}