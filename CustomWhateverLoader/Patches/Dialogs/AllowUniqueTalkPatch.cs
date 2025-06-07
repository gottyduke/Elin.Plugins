using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class AllowUniqueTalkPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaCustomSequence), nameof(DramaCustomSequence.Build))]
    internal static IEnumerable<CodeInstruction> OnCheckHumanSpeakIl(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Brfalse),
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new(OpCodes.Brfalse),
                new(OpCodes.Newobj));

        if (!cm.IsInvalid) {
            return cm
                .Advance(-1)
                .SetOpcodeAndAdvance(
                    OpCodes.Nop)
                .InstructionEnumeration();
        }

        // 23.149 Nightly introduced HasTopic & HumanSpeak check
        CwlMod.Log<AllowUniqueTalkPatch>($"cannot match HasTopic & HumanSpeak, game version {GameVersion.Normalized}");
        return cm.InstructionEnumeration();
    }
}