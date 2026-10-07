using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class AIActPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AIAct), nameof(AIAct.OnCancel))]
    internal static void OnCancel(AIAct __instance)
    {
        var owner = __instance.owner;
        if (!RuleKeywords.Has(owner?.id, Trigger.Battle | Trigger.Tail)) {
            return;
        }

        if (__instance is AI_Fuck fuck) {
            RuleState.TailingUids.Remove(fuck.owner.uid);
            if (fuck.target is { } target) {
                RuleState.TailingUids.Remove(target.uid);
            }
        }

        RuleState.JudgeQueue.Add(owner!.uid);
    }
}