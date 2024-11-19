using System.Collections.Generic;
using System.Reflection.Emit;
using EC.Components;
using HarmonyLib;

namespace EC.Patches;

[HarmonyPatch]
internal class ShowTooltipPatch
{
    [HarmonyTranspiler]
    // patch ShowTooltipForce? mayhaps
    [HarmonyPatch(typeof(UIButton), nameof(UIButton.ShowTooltip))]
    internal static IEnumerable<CodeInstruction> OnShowTooltipIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(TooltipManager),
                    nameof(TooltipManager.ShowTooltip))),
                new CodeMatch(OpCodes.Ret))
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(AuxTooltip.TryDrawAuxTooltip))
            .InstructionEnumeration();
    }
}