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
        var codes = new List<CodeInstruction>(instructions);
        if (codes.LastItem().opcode != OpCodes.Ret) {
            ECMod.Log($"failed to patch {nameof(UIButton.ShowTooltip)} due to unknown IL changes");
            return codes;
        }

        codes.InsertRange(codes.Count - 1,
        [
            new(OpCodes.Ldarg_0),
            Transpilers.EmitDelegate(AuxTooltip.TryDrawAuxTooltip),
        ]);

        return codes;
    }
}