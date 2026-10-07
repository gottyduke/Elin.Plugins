using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class ConditionPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Condition), nameof(Condition.Kill), typeof(bool))]
    internal static void OnKill(Condition __instance)
    {
        var owner = __instance.owner;
        if (owner is not null && EClass.core.IsGameStarted && RuleKeywords.Has(owner.id, Trigger.Condition)) {
            RuleState.JudgeQueue.Add(owner.uid);
        }
    }
}