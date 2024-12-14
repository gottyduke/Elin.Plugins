using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.API;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class RelaxedImportPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.Source.RelaxedImport?.Value ?? false;
    }
    
    [HarmonyTargetMethods]
    internal static IEnumerable<MethodInfo> SourcesCreateRow()
    {
        return typeof(SourceManager)
            .GetFields(AccessTools.all)
            .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
            .Select(f => f.FieldType)
            .Where(s => AccessTools.GetMethodNames(s).Any(mi => mi.Contains("CreateRow")))
            .Select(s => AccessTools.Method(s, "CreateRow"));
    }
    
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCreateSourceRowIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(o => o.opcode.ToString().Contains("ldc")),
                new CodeMatch(o => o.opcode == OpCodes.Call &&
                                   o.operand is MethodInfo mi &&
                                   mi.DeclaringType == typeof(SourceData)),
                new CodeMatch(OpCodes.Stfld))
            .Repeat(cm => {
                cm.Advance(1);
                
                var parser = cm.Instruction.operand as MethodInfo;
                cm.RemoveInstruction();
                
                var field = cm.Instruction.operand as FieldInfo;
                cm.SetInstructionAndAdvance(Transpilers.EmitDelegate<Action<object, int>>(
                    (row, id) => RelaxedImport(row, id, field!, parser!)));
            })
            .InstructionEnumeration();
    }

    private static void RelaxedImport(object row, int id, FieldInfo field, MethodInfo parser)
    {
        var sheet = SourceData.row.Sheet;
        var header = sheet.GetRow(sheet.FirstRowNum);
        
        var matched = header.Cells.FindAll(c => c.StringCellValue == field.Name);
        if (matched.Count != 0 && matched.All(c => c.ColumnIndex != id)) {
            var pos = matched[0].ColumnIndex;
            var value = SourceData.row.GetCell(pos).StringCellValue;
            var migrate = MigrateDetail.GetOrAdd(sheet.Workbook);
            
            CwlMod.Debug($"reloc {id}->{pos}:{field.Name}:{value}");
            id = pos;
        } else {
            CwlMod.Debug($"parse {id}:{field.Name}:{parser.Name}");
        }

        field.SetValue(row, parser.Invoke(null, [id]));
    }
}
