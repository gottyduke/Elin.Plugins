using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class GoalCombatPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GoalCombat), nameof(GoalCombat.Run))]
    internal static void OnRun(GoalCombat __instance)
    {
        if (RuleKeywords.Has(__instance.owner.id, Trigger.Battle)) {
            RuleState.JudgeQueue.Add(__instance.owner.uid);
        }
    }
}