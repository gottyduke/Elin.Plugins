using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace EC.Patches;

[HarmonyPatch]
internal class TooltipManagerPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TooltipManager), "Update")]
    internal static IEnumerable<CodeInstruction> OnUpdateIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(true,
                new CodeMatch(OpCodes.Ldfld,
                    AccessTools.Field(typeof(TooltipManager), nameof(TooltipManager.disableHide))),
                new CodeMatch(OpCodes.Call))
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate(DisabledOrAuxNote)
            )
            .InstructionEnumeration();
    }

    private static bool DisabledOrAuxNote(string lhs, string rhs)
    {
        return lhs == rhs || lhs.StartsWith("aux");
    }
}