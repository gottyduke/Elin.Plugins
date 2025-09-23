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
using NPOI.SS.UserModel;

namespace Cwl.Patches.Sources;

internal class NamedImportPatch
{
    private static readonly Dictionary<Type, Dictionary<string, int>> _expected = [];
    private static readonly Dictionary<ISheet, Dictionary<string, int>> _cached = [];

    internal static bool Prepare()
    {
        return CwlConfig.NamedImport;
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return WorkbookImporter.Sources!.Values
            .Select(MethodInfo? (sd) => sd?.GetType().GetRuntimeMethod("CreateRow", []))
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
                new OpCodeContains("ldc"),
                new OperandMatch(OpCodes.Call, o => o.operand is MethodInfo mi &&
                                                    mi.DeclaringType == typeof(SourceData)))
            .EnsureValid("ldc source data")
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
                var columnName = field!.Name;

                var id = ldc.opcode.Value switch {
                    >= 0x16 and <= 0x1E => ldc.opcode.Value - 0x16,
                    0x1F => (sbyte)ldc.operand,
                    _ => throw new SourceParseException($"invalid ID for {parser!.Name}:{columnName}"),
                };

                var rowType = rowCreator.DeclaringType!;

                cm.SetInstructionAndAdvance(Transpilers.EmitDelegate<Action<object>>(row =>
                    RelaxedImport(row, id, field, parser!, rowType, extraParser)));

                _expected.TryAdd(rowType, []);
                _expected[rowType][columnName] = id;
            })
            .InstructionEnumeration();
    }

    private static void RelaxedImport(object row,
                                      int id,
                                      FieldInfo field,
                                      MethodInfo parser,
                                      Type rowCreator,
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
        var migrate = MigrateDetail.GetFromWorkbook(sheet.Workbook);
        var expected = _expected[rowCreator];

        try {
            if (!_cached.TryGetValue(sheet, out var header)) {
                var headerColumns = sheet.GetRow(sheet.FirstRowNum).Cells
                    .Where(c => !c.StringCellValue.IsEmpty());

                header = new();
                foreach (var cell in headerColumns) {
                    // to mimic the sort & components override behaviour(bug) in Elin code
                    header[cell.StringCellValue.Trim()] = cell.ColumnIndex;
                }

                _cached[sheet] = header;
                migrate?.StartNewSheet(sheet, expected);

                var strategy = migrate?.CurrentSheet?.MigrateStrategy ?? MigrateDetail.Strategy.Unknown;
                if (strategy == MigrateDetail.Strategy.Unknown) {
                    strategy = expected.All(header.Contains) &&
                               expected.Count <= header.Count
                        ? MigrateDetail.Strategy.Correct
                        : MigrateDetail.Strategy.Missing;
                }

                migrate?.SetStrategy(strategy).SetGiven(header);
            }

            object? parsed = null;
            var useFallback = false;
            if (!header.TryGetValue(field.Name, out var existId)) {
                useFallback = FallbackDetail.Fallbacks.TryGetValue(field.FieldType, out var fallback);
                if (useFallback) {
                    parsed = fallback;
                } else {
                    existId = id;
                }
            }

            if (!useFallback) {
                parsed = extraParser is not null
                    ? extraParser.FastInvokeStatic(parser.FastInvokeStatic(existId, false)!)
                    : parser.FastInvokeStatic(existId);
            }

            if (parsed is IList<string> array) {
                for (var i = 0; i < array.Count; ++i) {
                    array[i] = CellPostProcessPatch.OnGetCell(array[i]) ?? array[i];
                }
            }

            field.SetValue(row, parsed);

            /*
            if (strategy == MigrateDetail.Strategy.Missing) {
                var parseDetail = readPos == id ? "cwl_import_parse" : "cwl_import_reloc";
                CwlMod.Debug($"{parseDetail.Loc(id, readPos)}:{field.Name}:{parser.Name}");
            }
            /**/
        } finally {
            if (id == expected.Count - 1) {
                migrate?.FinalizeMigration();
            }
        }
    }
}