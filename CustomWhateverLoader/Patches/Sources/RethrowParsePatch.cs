using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cwl.Helper.Runtime;
using Cwl.Helper.Runtime.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class RethrowParsePatch
{
    private static readonly Dictionary<int, MethodInfo> _cached = [];

    private static readonly string[] _methodNames = [
        nameof(SourceData.GetInt),
        nameof(SourceData.GetIntArray),
        nameof(SourceData.GetBool),
        nameof(SourceData.GetDouble),
        nameof(SourceData.GetFloat),
        nameof(SourceData.GetFloatArray),
        nameof(SourceData.GetString),
        nameof(SourceData.GetStringArray),
    ];

    internal static bool Prepare()
    {
        return CwlConfig.RethrowException;
    }

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        var parsers = typeof(SourceData).GetTypeInfo().GetCachedMethods()
            .Where(mi => mi.IsStatic && _methodNames.Contains(mi.Name))
            .Where(mi => mi.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(int)]));
        var excelParsers = typeof(ExcelParser).GetTypeInfo().GetCachedMethods();

        foreach (var parser in parsers) {
            var excelParser = Array.Find(excelParsers, mi => mi.Name == parser.Name);
            if (excelParser is null) {
                continue;
            }

            _cached[parser.MetadataToken] = excelParser;
            yield return parser;
        }
    }

    [Time]
    [HarmonyPrefix]
    internal static bool RethrowParseInvoke(int id, ref object? __result, MethodInfo __originalMethod)
    {
        try {
            if (!_cached.TryGetValue(__originalMethod.MetadataToken, out var parser)) {
                return true;
            }

            __result = parser.FastInvokeStatic(id);
        } catch (Exception ex) {
            var row = ExcelParser.row;
            var sb = new StringBuilder();

            var expectedType = __originalMethod.ReturnType.Name;
            var rawValue = row.Cells.TryGet(id, true);
            sb.AppendLine("cwl_error_source_rethrow".Loc(row.RowNum, id, ToLetterId(id), expectedType, rawValue));

            var defValue = row.RowNum < 4
                ? "cwl_error_source_rethrow_row".Loc()
                : "cwl_error_source_rethrow_def".Loc(ExcelParser.rowDefault.Cells.TryGet(id, true));
            sb.AppendLine(defValue);

            sb.AppendLine(ex.InnerException?.Message.SplitLines()[0]);

            throw new SourceParseException(sb.ToString(), ex);
        }

        return false;
    }

    private static string ToLetterId(int columnId)
    {
        var name = "";
        while (columnId >= 0) {
            name = (char)(columnId % 26 + 'A') + name;
            columnId /= 26;
            columnId--;
        }

        return name;
    }
}