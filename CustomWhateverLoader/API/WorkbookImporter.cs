using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using NPOI.XSSF.UserModel;

namespace Cwl.API;

public class WorkbookImporter
{
    private static List<FieldInfo> Sources => typeof(SourceManager)
        .GetFields(AccessTools.all)
        .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
        .ToList();

    [Time]
    public static IEnumerable<SourceData?> BySheetName(FileInfo? import)
    {
        if (import?.FullName is null or "") {
            return [];
        }

        using var fs = File.OpenRead(import.FullName);
        var book = new XSSFWorkbook(fs);
        MigrateDetail.GetOrAdd(book).SetFile(import.GetFullFileNameWithoutExtension());

        List<SourceData> dirty = [];

        for (var i = 0; i < book.NumberOfSheets; ++i) {
            try {
                var sheet = book.GetSheetAt(i);

                var sourceField = Sources.FirstOrDefault(f => f.FieldType.Name == $"Source{sheet.SheetName}" ||
                                                              f.FieldType.Name == $"Lang{sheet.SheetName}");
                if (sourceField is null) {
                    CwlMod.Log("cwl_log_sheet_skip".Loc(sheet.SheetName));
                    continue;
                }

                var sheetName = $"{sourceField.Name}:{import.Name}/{sheet.SheetName}";
                CwlMod.Log("cwl_log_sheet".Loc(sheet.SheetName));

                if (sourceField.GetValue(EMono.sources) is not SourceData source ||
                    !source.ImportData(sheet, import.Name, true)) {
                    throw new SourceParseException("cwl_error_source_except".Loc(sheetName));
                }

                dirty.Add(source);
            } catch (Exception ex) {
                CwlMod.Error("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }

        return dirty;
    }
}