using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Migration;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class RethrowParsePatch
{
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
        return typeof(SourceData).GetTypeInfo().GetCachedMethods()
            .Where(mi => mi.IsStatic && _methodNames.Contains(mi.Name) &&
                         mi.ValidateParameterTypes(false, typeof(int)));
    }

    [Time]
    [HarmonyFinalizer]
    internal static Exception? RethrowParseInvoke(Exception? __exception, MethodInfo __originalMethod, int id)
    {
        if (__exception is null) {
            return null;
        }

        var row = ExcelParser.row;
        using var sb = StringBuilderPool.Get();

        if (SourceInitPatch.SafeToCreate && MigrateDetail.CurrentDetail is { } detail) {
            sb.Append($"{detail.Mod?.id.TagColor(0x2f2d2d)} // ");
            sb.AppendLine(detail.CurrentSheet?.Sheet?.SheetName?.TagColor(0x7676a7));
            sb.AppendLine(detail.SheetFile.ShortPath());
        }

        var expectedType = __originalMethod.ReturnType.Name;
        var rawValue = row.Cells.TryGet(id, true);
        sb.Append("cwl_error_source_rethrow".Loc(row.RowNum + 1, id + 1, ToLetterId(id), expectedType, rawValue));

        var defValue = row.RowNum < 3
            ? "cwl_error_source_rethrow_row".Loc()
            : "cwl_error_source_rethrow_def".Loc(ExcelParser.rowDefault.Cells.TryGet(id, true));
        sb.AppendLine(defValue);
        sb.AppendLine(__exception.GetType().Name);

        return new SourceParseException(sb.ToString(), __exception);
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