using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace MG.Patches;

[HarmonyPatch]
internal class EnchantCategoryPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetEnchant))]
    internal static IEnumerable<CodeInstruction> OnSetCategoryFlagIl(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldstr),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Brtrue))
            .Insert(
                cm.InstructionsInRange(cm.Pos, cm.Pos + 3))
            .Advance(1)
            .SetOperandAndAdvance("ability")
            .InstructionEnumeration();
    }
}