using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class OverrideRewardTextPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Quest), nameof(Quest.GetRewardText))]
    internal static bool OnGetRewardText(Quest __instance, ref string __result)
    {
        if (__instance is not CustomQuest cm) {
            return true;
        }

        __result = cm.GetRewardText();
        return false;
    }
}