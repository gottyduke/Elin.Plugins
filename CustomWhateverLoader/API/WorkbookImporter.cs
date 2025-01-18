using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.API.Migration;
using Cwl.API.Processors;
using Cwl.Helper.Runtime;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Cwl.API;

public class WorkbookImporter
{
    private static FieldInfo[]? _sources;

    internal static FieldInfo[] Sources => _sources ??= typeof(SourceManager)
        .GetFields(AccessTools.all)
        .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
        .ToArray();

    [Time]
    public static IEnumerable<SourceData?> BySheetName(FileInfo? import)
    {
        if (import?.FullName is null or "") {
            return [];
        }

        using var fs = File.Open(import.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var book = new XSSFWorkbook(fs);
        MigrateDetail.GetOrAdd(book).SetFile(import.GetFullFileNameWithoutExtension());

        List<SourceData> dirty = [];

        WorkbookProcessor.PreProcess(book);

        List<ISheet> orderedSheets = [];
        for (var i = 0; i < book.NumberOfSheets; ++i) {
            var sheet = book.GetSheetAt(i);
            if (sheet.SheetName is "Element" or "Material") {
                orderedSheets.Insert(0, sheet);
            } else {
                orderedSheets.Add(sheet);
            }
        }

        foreach (var sheet in orderedSheets) {
            try {
                var sourceField = Sources.FirstOrDefault(f => f.FieldType.Name == $"Source{sheet.SheetName}" ||
                                                              f.FieldType.Name == $"Lang{sheet.SheetName}");
                if (sourceField is null) {
                    CwlMod.Log<WorkbookImporter>("cwl_log_sheet_skip".Loc(sheet.SheetName));
                    continue;
                }

                SheetProcessor.PreProcess(sheet);

                var sheetName = $"{sourceField.Name}:{import.Name}/{sheet.SheetName}";
                CwlMod.Log<WorkbookImporter>("cwl_log_sheet".Loc(sheet.SheetName));

                if (sourceField.GetValue(EMono.sources) is not SourceData source ||
                    !source.ImportData(sheet, import.Name, true)) {
                    throw new SourceParseException("cwl_error_source_except".Loc(sheetName));
                }

                SheetProcessor.PostProcess(sheet);

                dirty.Add(source);
            } catch (Exception ex) {
                CwlMod.Error<WorkbookImporter>("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }

        WorkbookProcessor.PostProcess(book);

        return dirty;
    }

    public static void CacheSheet()
    {
    }
}