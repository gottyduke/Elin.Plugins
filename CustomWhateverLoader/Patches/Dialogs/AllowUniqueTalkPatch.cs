using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class AllowUniqueTalkPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaCustomSequence), nameof(DramaCustomSequence.Build))]
    internal static IEnumerable<CodeInstruction> OnCheckHumanSpeakIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Brfalse),
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new(OpCodes.Brfalse),
                new(OpCodes.Newobj))
            .ThrowIfInvalid("failed to match hasTopic & humanSpeak")
            .Advance(-1)
            .SetOpcodeAndAdvance(OpCodes.Pop)
            .InstructionEnumeration();
    }
}