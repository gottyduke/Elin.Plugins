using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.API.Migration;
using Cwl.API.Processors;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Cwl.API;

public class WorkbookImporter
{
    internal static readonly MethodBase Importer = AccessTools.Method(
        typeof(WorkbookImporter),
        nameof(BySheetName), [typeof(ISheet), typeof(string)]);

    private static FieldInfo[]? _sources;

    internal static FieldInfo[] Sources => _sources ??= typeof(SourceManager)
        .GetFields(AccessTools.all)
        .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
        .ToArray();

    // all sheets from workbook
    public static IEnumerable<SourceData?> BySheetName(IWorkbook book, string bookName, string[]? fetched = null)
    {
        List<SourceData> dirty = [];
        HashSet<string> excludedSheet = [..fetched ?? []];

        WorkbookProcessor.PreProcess(book);

        for (var i = 0; i < book.NumberOfSheets; ++i) {
            var sheet = book.GetSheetAt(i);
            var sheetName = sheet.SheetName;
            if (excludedSheet.Contains(sheetName)) {
                continue;
            }

            var source = BySheetName(sheet, bookName);
            if (source is null) {
                continue;
            }

            dirty.Add(source);
        }

        WorkbookProcessor.PostProcess(book);

        return dirty;
    }

    // single sheet
    public static SourceData? BySheetName(ISheet sheet, string bookName)
    {
        var sheetName = sheet.SheetName;
        try {
            var sourceField = Sources.FirstOrDefault(f => f.FieldType.Name == $"Source{sheetName}" ||
                                                          f.FieldType.Name == $"Lang{sheetName}");
            if (sourceField?.GetValue(EMono.sources) is not SourceData source) {
                CwlMod.Log<WorkbookImporter>("cwl_log_sheet_skip".Loc(sheetName));
                return null;
            }

            SheetProcessor.PreProcess(sheet);

            CwlMod.Log<WorkbookImporter>("cwl_log_sheet".Loc(sheetName));

            var sheetFullName = $"{sourceField.Name}:{bookName}/{sheetName}";
            if (!source.ImportData(sheet, bookName, true)) {
                throw new SourceParseException("cwl_error_source_except".Loc(sheetFullName));
            }

            SheetProcessor.PostProcess(sheet);

            return source;
        } catch (Exception ex) {
            CwlMod.ErrorWithPopup<WorkbookImporter>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return null;
    }

    public static void LoadAllFiles(IEnumerable<FileInfo> imports, string prefetch = nameof(Element))
    {
#if DEBUG
        var alloc = GC.GetTotalMemory(true);
#endif

        var usePrefetch = true;
        var chunkSize = CwlConfig.MaxPrefetchLoads;
        if (chunkSize == -1) {
            usePrefetch = false;
            chunkSize = int.MaxValue;
        }

        List<IWorkbook> books = [];
        List<(ISheet, string)> fetches = [];
        HashSet<SourceData> dirty = [EMono.sources.elements, EMono.sources.materials];

        // init elements and materials first so other sheets can parse it properly
        HotInit(dirty);

        var files = imports.ToArray();
        for (var i = 0; i < files.Length; i += chunkSize) {
            foreach (var file in files.Skip(i).Take(chunkSize)) {
                using var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var book = new XSSFWorkbook(fs);
                MigrateDetail.GetOrAdd(book)
                    .SetFile(file.GetFullFileNameWithoutExtension())
                    .SetMod(BaseModManager.Instance.packages.LastOrDefault(p => file.FullName.StartsWith(p.dirInfo.FullName)));

                books.Add(book);

                // setup prefetches
                for (var j = 0; j < book.NumberOfSheets; ++j) {
                    var sheet = book.GetSheetAt(j);
                    var sheetName = sheet.SheetName;
                    if (sheetName == prefetch) {
                        fetches.Add((sheet, file.Name));
                    }
                }
            }

            if (usePrefetch) {
                // load prefetched sheets
                foreach (var (fetch, bookName) in fetches) {
                    CwlMod.Log<SourceManager>("cwl_log_workbook".Loc(bookName));

                    var source = BySheetName(fetch, bookName);
                    if (source is not null) {
                        dirty.Add(source);
                    }
                }

                // init prefetched
                HotInit(dirty);
            } else {
                prefetch = "";
            }

            // load regular book
            foreach (var book in books) {
                try {
                    var migration = MigrateDetail.GetOrAdd(book);
                    var fileName = migration.SheetFile;
                    CwlMod.Log<SourceManager>("cwl_log_workbook".Loc(fileName.ShortPath()));

                    var sources = BySheetName(book, Path.GetFileName(fileName), [prefetch])
                        .OfType<SourceData>();
                    dirty.UnionWith(sources);
                } catch (Exception ex) {
                    CwlMod.WarnWithPopup<SourceManager>("cwl_error_failure".Loc(ex.Message), ex);
                    // noexcept
                }
            }

            HotInit(dirty);
        }

#if DEBUG
        var allocNew = GC.GetTotalMemory(true);
        var allocDiff = allocNew - alloc;
        CwlMod.Debug<WorkbookImporter>($"prefetch chunk size: {chunkSize} | mem alloc: {allocDiff.ToAllocateString()}");
#endif

        MigrateDetail.DumpTiming();
        MigrateDetail.Clear();
    }

    private static void HotInit(IEnumerable<SourceData> sources)
    {
        foreach (var imported in sources) {
            try {
                imported.Reset();
                // 1.18.12 new AllowHotInitialization prevents setting rows before Init...
                imported.Init();
            } catch (Exception ex) {
                CwlMod.ErrorWithPopup<WorkbookImporter>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }
}