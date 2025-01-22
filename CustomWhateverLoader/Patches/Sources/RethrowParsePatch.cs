using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cwl.Helper.Runtime;
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
        var mi = typeof(SourceData).GetTypeInfo().GetCachedMethods()
            .Where(mi => mi.IsStatic && _methodNames.Contains(mi.Name))
            .Where(mi => mi.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(int)]));
        return mi;
    }

    [Time]
    [HarmonyPrefix]
    internal static bool RethrowParseInvoke(int id, ref object? __result, MethodInfo __originalMethod)
    {
        try {
            if (!_cached.TryGetValue(__originalMethod.MetadataToken, out var parser)) {
                parser = AccessTools.FirstMethod(typeof(ExcelParser), mi => mi.Name == __originalMethod.Name);
                _cached[__originalMethod.MetadataToken] = parser;
            }

            __result = parser.FastInvokeStatic(id);
        } catch (Exception ex) {
            var row = ExcelParser.row;
            var sb = new StringBuilder();

            var expected = __originalMethod.ReturnType;
            sb.Append($"\nrow#{row.RowNum}, cell#{id}/{ToLetterId(id)}, expected:{expected.Name}, raw:{row.Cells[id]}");

            sb.AppendLine(row.RowNum < 4
                ? ", SourceData begins at the 4th row. 3rd row is expected to be the default value row."
                : $", default:{ExcelParser.rowDefault.Cells[id]}");

            sb.AppendLine(ex.InnerException?.Message.SplitNewline()[0]);

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