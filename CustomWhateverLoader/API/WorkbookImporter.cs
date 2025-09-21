using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.API.Migration;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
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

    internal static Dictionary<string, SourceData?>? Sources =>
        field ??= typeof(SourceManager)
            .GetFields(AccessTools.all)
            .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
            .ToDictionary(f => f.FieldType.Name, f => f.GetValue(EMono.sources) as SourceData);

    public static SourceData? FindSourceByName(string name)
    {
        return Sources.GetValueOrDefault($"Source{name}", Sources.GetValueOrDefault($"Lang{name}"));
    }

    // all sheets from workbook
    public static IEnumerable<SourceData?> BySheetName(IWorkbook book, FileInfo file, string[]? fetched = null)
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

            var (source, _) = BySheetName(sheet, file);
            if (source is null) {
                continue;
            }

            dirty.Add(source);
        }

        WorkbookProcessor.PostProcess(book);

        return dirty;
    }

    // single sheet
    public static (SourceData?, SourceData.BaseRow[]) BySheetName(ISheet sheet, string bookName)
    {
        var sheetName = sheet.SheetName;
        try {
            var source = FindSourceByName(sheetName);
            if (source is null) {
                return new();
            }

            SheetProcessor.PreProcess(sheet);

            CwlMod.Log<WorkbookImporter>("cwl_log_sheet".Loc(sheetName));

            IList? rows;
            // ThingV resets rows
            if (source is SourceThingV) {
                rows = EMono.sources.things.rows;
            } else {
                rows = source.GetFieldValue("rows") as IList;
            }

            var begin = rows!.Count;

            if (!source.ImportData(sheet, bookName, true)) {
                var sheetFullName = $"{source.GetType().Name}:{bookName}/{sheetName}";
                throw new SourceParseException("cwl_error_source_except".Loc(sheetFullName));
            }

            SourceData.BaseRow[] imported = [];
            var added = rows.Count - begin;
            if (added > 0) {
                imported = rows
                    .OfType<SourceData.BaseRow>()
                    .Skip(begin)
                    .Take(added)
                    .ToArray();
            }

            SheetProcessor.PostProcess(sheet);

            return (source, imported);
        } catch (Exception ex) {
            CwlMod.ErrorWithPopup<WorkbookImporter>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return new();
    }

    // load using cache
    public static (SourceData?, SourceData.BaseRow[]) BySheetName(ISheet sheet, FileInfo file, bool useCache = true)
    {
        var sheetName = sheet.SheetName;
        CacheDetail? detail = null;
        (SourceData?, SourceData.BaseRow[]) data = new();

        if (useCache) {
            SheetProcessor.PreProcess(sheet);

            detail = CacheDetail.GetOrAdd(file);
            if (detail.TryGetCache(sheetName, out data.Item2)) {
                CwlMod.Log<WorkbookImporter>("cwl_log_sheet".Loc(sheetName));

                data.Item1 = FindSourceByName(sheetName);
                // ThingV resets rows
                if (data.Item1 is SourceThingV) {
                    data.Item1 = EMono.sources.things;
                }

                var imported = data.Item1?.ImportRows(data.Item2);
                CwlMod.Log<WorkbookImporter>($"{sheetName}/{imported}");
            }

            SheetProcessor.PostProcess(sheet);
        }

        if (data.Item1 is not null) {
            return data;
        }

        data = BySheetName(sheet, file.Name);
        if (useCache) {
            detail?.EmplaceCache(sheetName, data.Item2 ?? []);
        }

        return data;
    }

    public static void LoadAllFiles(IEnumerable<FileInfo> imports, string prefetch = nameof(Element))
    {
        var alloc = GC.GetTotalMemory(false);

        var usePrefetch = true;
        var useCache = CwlConfig.CacheSourceSheets;
        var chunkSize = CwlConfig.MaxPrefetchLoads;
        if (chunkSize == -1) {
            usePrefetch = false;
            chunkSize = int.MaxValue;
        }

        CwlMod.Debug<WorkbookImporter>($"prefetch enabled: {usePrefetch}");
        CwlMod.Debug<WorkbookImporter>($"cache enabled: {useCache}");

        List<IWorkbook> books = [];
        List<(ISheet, FileInfo)> fetches = [];
        var sm = EMono.sources;
        HashSet<SourceData> dirty = [sm.elements, sm.materials];

        // init elements and materials first so other sheets can parse it properly
        HotInit(dirty);

        var files = imports.ToArray();
        for (var i = 0; i < files.Length; i += chunkSize) {
            foreach (var file in files.Skip(i).Take(chunkSize)) {
                using var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var book = new XSSFWorkbook(fs);
                MigrateDetail.GetOrAdd(book)
                    .SetFile(file)
                    .SetMod(BaseModManager.Instance.packages.LastOrDefault(p => file.FullName.StartsWith(p.dirInfo.FullName)));

                books.Add(book);

                // setup prefetches
                for (var j = 0; j < book.NumberOfSheets; ++j) {
                    var sheet = book.GetSheetAt(j);
                    var sheetName = sheet.SheetName;
                    if (sheetName == prefetch) {
                        fetches.Add((sheet, file));
                    }
                }
            }

            if (usePrefetch) {
                // load prefetched sheets
                foreach (var (fetch, file) in fetches) {
                    CwlMod.Log<SourceManager>("cwl_log_workbook".Loc(file.Name));

                    var (source, _) = BySheetName(fetch, file);
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

                    var sources = BySheetName(book, fileName, [prefetch])
                        .OfType<SourceData>();
                    dirty.UnionWith(sources);
                } catch (Exception ex) {
                    CwlMod.WarnWithPopup<SourceManager>("cwl_error_failure".Loc(ex.Message), ex);
                    // noexcept
                }
            }

            HotInit(dirty);
        }

        var allocNew = GC.GetTotalMemory(false);
        var allocDiff = allocNew - alloc;
        CwlMod.Debug<WorkbookImporter>($"prefetch chunk size: {chunkSize} | mem alloc: {allocDiff.ToAllocateString()}");

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