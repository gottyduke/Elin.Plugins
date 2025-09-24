using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class AllowUniqueTalkPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaCustomSequence), nameof(DramaCustomSequence.Build))]
    internal static IEnumerable<CodeInstruction> OnCheckHumanSpeakIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Callvirt, nameof(Chara.IsHumanSpeak)))
            .EnsureValid("humanSpeak")
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate(HasUniqueTalk))
            .InstructionEnumeration();
    }

    private static bool HasUniqueTalk(Chara chara)
    {
        return chara.IsHumanSpeak || chara.HasRumorText("unique");
    }
}