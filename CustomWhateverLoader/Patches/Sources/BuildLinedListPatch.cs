using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class BuildLinedListPatch
{
    // prone to break
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ExcelData), nameof(ExcelData.BuildList))]
    internal static IEnumerable<CodeInstruction> OnAddLineIl(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm
            .End()
            .MatchStartBackwards(
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OperandContains(OpCodes.Callvirt, nameof(IList<IDictionary<string, string>>.Add)),
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Add),
                new OpCodeContains(nameof(OpCodes.Stloc)))
            .ThrowIfInvalid("failed to match add list")
            .Advance(1)
            .RemoveInstruction()
            .InsertAndAdvance(
                new(cm.Instruction.opcode),
                Transpilers.EmitDelegate(AddAndSetLine))
            .InstructionEnumeration();
    }

    private static void AddAndSetLine(IList<IDictionary<string, string>> list, IDictionary<string, string> dict, int rowNum)
    {
        dict["cwl_row_num"] = (rowNum + 1).ToString();
        list.Add(dict);
    }
}