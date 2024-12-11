using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.API;
using HarmonyLib;

namespace Cwl.Patches;

[HarmonyPatch]
internal class RethrowParsePatch
{
    [HarmonyTargetMethods]
    internal static IEnumerable<MethodInfo> SourceDataCellParsers()
    {
        return [
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetInt), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetIntArray), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetBool), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetDouble), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetFloat), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetFloatArray), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetString), [typeof(int)]),
            AccessTools.Method(typeof(SourceData), nameof(SourceData.GetStringArray), [typeof(int)]),
        ];
    }

    [HarmonyPrefix]
    internal static bool ParseRethrowInvoke(int id, ref object __result, MethodInfo __originalMethod)
    {
        var parser = AccessTools.FirstMethod(typeof(ExcelParser), mi => mi.Name == __originalMethod.Name);
        try {
            __result = parser.Invoke(null, [id]);
        } catch (Exception ex) {
            var row = ExcelParser.row;
            var details = $"row#{row.RowNum}, cell#{id}, expected:{parser.ReturnType.Name}, raw:{row.Cells[id]}";
            var message = ex.InnerException?.Message.SplitNewline()[0];
            throw new SourceParseException($"{message}\n{details}", ex);
        }

        return false;
    }
}