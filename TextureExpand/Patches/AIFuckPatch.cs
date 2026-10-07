using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class AIFuckPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AI_Fuck), nameof(AI_Fuck.Run))]
    internal static IEnumerable<CodeInstruction> OnRunIl(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(OpCodes.Stfld));

        if (matcher.IsInvalid) {
            TextureExpand.LogError("failed to match the start of AI_Fuck.Run, #tail is disabled");
            return matcher.InstructionEnumeration();
        }

        return matcher
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(OnStart))
            .InstructionEnumeration();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AI_Fuck), nameof(AI_Fuck.Finish))]
    internal static void OnFinish(AI_Fuck __instance)
    {
        if (__instance.owner is { } owner && TryQueueTail(owner)) {
            RuleState.TailingUids.Remove(owner.uid);
        }

        if (__instance.target is { } target && TryQueueTail(target)) {
            RuleState.TailingUids.Remove(target.uid);
        }
    }

    private static void OnStart(AI_Fuck act)
    {
        if (TryQueueTail(act.owner)) {
            RuleState.TailingUids.Add(act.owner.uid);
        }

        if (act.target is { } target && TryQueueTail(target)) {
            RuleState.TailingUids.Add(target.uid);
        }
    }

    private static bool TryQueueTail(Chara chara)
    {
        if (!RuleKeywords.Has(chara.id, Trigger.Tail)) {
            return false;
        }

        RuleState.JudgeQueue.Add(chara.uid);
        return true;
    }
}