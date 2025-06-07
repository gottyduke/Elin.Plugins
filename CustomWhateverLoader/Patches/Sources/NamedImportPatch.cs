using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Migration;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using HarmonyLib;
using MethodTimer;
using NPOI.SS.UserModel;

namespace Cwl.Patches.Sources;

internal class NamedImportPatch
{
    private static readonly Dictionary<Type, List<MigrateDetail.HeaderCell>> _expected = [];
    private static readonly Dictionary<ISheet, List<MigrateDetail.HeaderCell>> _cached = [];

    internal static bool Prepare()
    {
        return CwlConfig.NamedImport;
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return WorkbookImporter.Sources
            .Select(MethodInfo? (sf) => sf.FieldType.GetRuntimeMethod("CreateRow", []))
            .OfType<MethodInfo>()
            .Distinct(OverrideMethodComparer.Default);
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCreateSourceRowIl(IEnumerable<CodeInstruction> instructions,
        MethodBase rowCreator)
    {
        var miGetStr = AccessTools.Method(typeof(SourceData), nameof(SourceData.GetStr));
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new(o => o.opcode.ToString().Contains("ldc")),
                new OperandMatch(OpCodes.Call, o => o.operand is MethodInfo mi &&
                                                    mi.DeclaringType == typeof(SourceData)))
            .Repeat(cm => {
                var extraParse = false;
                // Core.ParseElements
                if ((MethodInfo)cm.InstructionAt(1).operand == miGetStr) {
                    cm.RemoveInstruction();
                    cm.Advance(-1);
                    extraParse = true;
                }

                var ldc = cm.Instruction;
                cm.RemoveInstruction();

                var parser = cm.Instruction.operand as MethodInfo;
                cm.RemoveInstruction();

                MethodInfo? extraParser = null;
                if (extraParse) {
                    extraParser = cm.Operand as MethodInfo;
                    cm.RemoveInstruction();
                }

                var field = cm.Instruction.operand as FieldInfo;

                var id = ldc.opcode.Value switch {
                    >= 0x16 and <= 0x1E => ldc.opcode.Value - 0x16,
                    0x1F => (sbyte)ldc.operand,
                    _ => throw new SourceParseException($"invalid ID for {parser!.Name}:{field!.Name}"),
                };

                var rowType = rowCreator.DeclaringType!;

                cm.SetInstructionAndAdvance(Transpilers.EmitDelegate<Action<object>>(row =>
                    RelaxedImport(row, id, field!, parser!, rowType, extraParser)));

                _expected.TryAdd(rowType, []);
                if (_expected[rowType].All(c => c.Index != id)) {
                    _expected[rowType].Add(new(id, field!.Name));
                }
            })
            .InstructionEnumeration();
    }

    [Time]
    private static void RelaxedImport(object row, int id, FieldInfo field, MethodInfo parser, Type rowCreator,
        MethodInfo? extraParser)
    {
        if (!SourceInitPatch.SafeToCreate) {
            var parsed = extraParser is not null
                ? extraParser.FastInvokeStatic(parser.FastInvokeStatic(id, false)!)
                : parser.FastInvokeStatic(id);
            field.SetValue(row, parsed);
            return;
        }

        var sheet = SourceData.row.Sheet;
        var migrate = MigrateDetail.GetOrAdd(sheet.Workbook);
        var expected = _expected[rowCreator];

        try {
            if (!_cached.TryGetValue(sheet, out var header)) {
                header = sheet.GetRow(sheet.FirstRowNum).Cells
                    .Where(c => !c.StringCellValue.IsEmpty())
                    .Select(c => new MigrateDetail.HeaderCell(c.ColumnIndex, c.StringCellValue.Trim()))
                    .ToList();

                _cached[sheet] = header;
                migrate.StartNewSheet(sheet, expected);
            }

            var headerSet = new HashSet<MigrateDetail.HeaderCell>(header);
            var strategy = expected.All(headerSet.Contains)
                ? MigrateDetail.Strategy.Correct
                : MigrateDetail.Strategy.Missing;

            var readPos = id;
            if (strategy == MigrateDetail.Strategy.Missing) {
                var expectedFlat = expected.GroupBy(c => c.Name).ToDictionary(c => c.Key, c => c.Count());
                var headerFlat = header.GroupBy(c => c.Name).ToDictionary(c => c.Key, c => c.Count());

                if (header.Count == expected.Count &&
                    expectedFlat.All(c => headerFlat.TryGetValue(c.Key, out var count) && count == c.Value)) {
                    strategy = MigrateDetail.Strategy.Reorder;

                    var matched = header.FindAll(c => c.Name == field.Name);
                    if (matched.Count != 0 && matched.All(c => c.Index != id)) {
                        readPos = matched[0].Index;
                    }
                }

                migrate.SetStrategy(strategy).SetGiven(header);
            }

            var parsed = extraParser is not null
                ? extraParser.FastInvokeStatic(parser.FastInvokeStatic(readPos, false)!)
                : parser.FastInvokeStatic(readPos);
            field.SetValue(row, parsed);

            //var parseDetail = readPos == id ? "cwl_import_parse" : "cwl_import_reloc";
            //CwlMod.Debug($"{parseDetail.Loc(id, readPos)}:{field.Name}:{parser.Name}");
        } finally {
            if (id == expected.Count - 1) {
                migrate.FinalizeMigration();
            }
        }
    }
}