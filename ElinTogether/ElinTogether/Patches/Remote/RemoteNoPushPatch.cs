using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemoteNoPushPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara._Move))]
    internal static IEnumerable<CodeInstruction> OnCharaMoveByForceIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Beq))
            .EnsureValid("unconditional force move")
            .InsertAndAdvance(
                new(OpCodes.Pop),
                new(OpCodes.Pop))
            .SetOpcodeAndAdvance(OpCodes.Br)
            .InstructionEnumeration();
    }
}