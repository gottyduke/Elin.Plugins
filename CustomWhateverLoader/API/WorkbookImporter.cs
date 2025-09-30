using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        nameof(BySheetName));

    internal static string LastTiming = "";

    internal static Dictionary<string, SourceData?>? Sources =>
        field ??= typeof(SourceManager)
            .GetFields(AccessTools.all)
            .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
            .ToDictionary(f => f.FieldType.Name, f => f.GetValue(EMono.sources) as SourceData);

    /// <summary>
    ///     Find a SourceData by its name
    /// </summary>
    /// <param name="name">String name of the SourceData</param>
    /// <returns>SourceData if found</returns>
    public static SourceData? FindSourceByName(string name)
    {
        return Sources.GetValueOrDefault($"Source{name}",
            Sources.GetValueOrDefault($"Lang{name}", Sources.GetValueOrDefault(name)));
    }

    /// <summary>
    ///     Import a single sheet by its name that matches a SourceData or SourceLang
    /// </summary>
    /// <param name="sheet">Sheet to import</param>
    /// <param name="file">Useless</param>
    /// <returns>SourceData of the sheet and the rows imported</returns>
    /// <exception cref="SourceParseException">Any error occurred during the parsing</exception>
    public static (SourceData?, SourceData.BaseRow[]) BySheetName(ISheet sheet, FileInfo file)
    {
        var sheetName = sheet.SheetName;
        try {
            var source = FindSourceByName(sheetName);
            if (source is null) {
                return new();
            }

            var migrate = MigrateDetail.GetOrAdd(file);

            SheetProcessor.PreProcess(sheet);

            CwlMod.CurrentLoading = "cwl_log_sheet".Loc(sheetName);
            CwlMod.Log<WorkbookImporter>(CwlMod.CurrentLoading);

            IList? rows;
            // ThingV resets rows
            if (source is SourceThingV) {
                rows = EMono.sources.things.rows;
            } else {
                rows = source.GetFieldValue("rows") as IList;
            }

            var begin = rows!.Count;

            if (!source.ImportData(sheet, file.Name, true)) {
                var sheetFullName = $"{source.GetType().Name}:{file.Name}/{sheetName}";
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

            migrate.FinalizeMigration();

            return (source, imported);
        } catch (Exception ex) {
            CwlMod.ErrorWithPopup<WorkbookImporter>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return new();
    }

    /// <summary>
    ///     Import all workbook files
    /// </summary>
    /// <param name="imports">Files to import</param>
    public static void LoadAllFiles(FileInfo[] imports)
    {
        var useCache = CwlConfig.CacheSourceSheets;
        CwlMod.Debug<WorkbookImporter>($"cache enabled: {useCache}");

        var sm = EMono.sources;
        HashSet<SourceData> dirty = [sm.elements, sm.materials];

        // init elements and materials first so other sheets can parse it properly
        HotInit(dirty);

        var files = imports.ToArray();

        // load using cached or legacy loader
        var loadedSources = useCache
            ? LoadAllFilesCached(files)
            : LoadAllFilesLegacy(files);

        dirty.UnionWith(loadedSources);

        // init dirty data
        HotInit(dirty);

        // log timings
        foreach (var file in files) {
            ExecutionAnalysis.MethodTimeLogger.Log(Importer, TimeSpan.FromTicks(MigrateDetail.GetOrAdd(file).LoadingTime), "");
        }

        LastTiming = MigrateDetail.DumpTiming();
        MigrateDetail.Clear();

        CwlMod.Log<MigrateDetail>(LastTiming);
    }

    /// <summary>
    ///     Load all files using cache blobs if available, and the rest using old NPOI with Elin parser. <br />
    ///     Imported sheets will be cached.
    /// </summary>
    /// <param name="imports">Files to import</param>
    public static IEnumerable<SourceData> LoadAllFilesCached(FileInfo[] imports)
    {
        var caches = imports
            .Select(CacheDetail.GetOrAdd)
            .Distinct()
            .ToArray();
        var prefetches = caches
            .Where(c => c.DirtyOrEmpty)
            .Select(c => PrefetchWorkbook(c.SheetFile))
            .ToArray();
        var imported = prefetches
            .ToDictionary(p => CacheDetail.GetOrAdd(p.File));

        var elements = EMono.sources.elements;
        HashSet<SourceData?> dirty = [elements];

        // prefetch elements
        foreach (var cache in caches) {
            var fileName = cache.SheetFile.ShortPath();
            if (cache.DirtyOrEmpty) {
                if (!imported.TryGetValue(cache, out var prefetch) || prefetch.Element is null) {
                    continue;
                }

                CwlMod.Log<WorkbookImporter>("cwl_log_workbook".Loc(fileName));

                var (_, rows) = BySheetName(prefetch.Element, prefetch.File);

                cache.EmplaceCache(nameof(Element), rows);
            } else if (cache.TryGetCache(nameof(Element), out var rows)) {
                CwlMod.Log<WorkbookImporter>("cwl_log_workbook_cache".Loc(fileName));

                elements.ImportRows(rows);
            }
        }

        // add regular rows
        foreach (var cache in caches) {
            var fileName = cache.SheetFile.ShortPath();
            if (cache.DirtyOrEmpty) {
                if (!imported.TryGetValue(cache, out var prefetch) || prefetch.Sheets.Length == 0) {
                    continue;
                }

                CwlMod.Log<WorkbookImporter>("cwl_log_workbook".Loc(fileName));

                foreach (var sheet in prefetch.Sheets) {
                    if (sheet.SheetName == nameof(Element)) {
                        continue;
                    }

                    var (sourceData, rows) = BySheetName(sheet, prefetch.File);

                    cache.EmplaceCache(sheet.SheetName, rows);
                    dirty.Add(sourceData);
                }
            } else {
                CwlMod.Log<WorkbookImporter>("cwl_log_workbook_cache".Loc(fileName));

                foreach (var (type, rowsCached) in cache.Source) {
                    var sourceData = FindSourceByName(type);
                    switch (sourceData) {
                        // ThingV resets rows
                        case SourceThingV:
                            sourceData = EMono.sources.things;
                            break;
                        case SourceElement or null:
                            continue;
                    }

                    sourceData.ImportRows(rowsCached);
                    dirty.Add(sourceData);
                }
            }
        }

        return dirty.OfType<SourceData>();
    }

    /// <summary>
    ///     Load all excel books using the old NPOI with Elin parser. <br />
    ///     Extremely slow. Use this if you need to grab a cup of coffee.
    /// </summary>
    /// <param name="imports">Files to import</param>
    public static IEnumerable<SourceData> LoadAllFilesLegacy(FileInfo[] imports)
    {
        var alloc = GC.GetTotalMemory(false);

        HashSet<SourceData?> dirty = [];
        var prefetches = imports
            .Select(PrefetchWorkbook)
            .ToArray();

        // prefetch elements
        foreach (var (file, _, element) in prefetches) {
            if (element is null) {
                continue;
            }

            CwlMod.Log<SourceManager>($"prefetch workbook sheet: {file.ShortPath()}");

            var (source, _) = BySheetName(element, file);
            dirty.Add(source);
        }

        // load regular sheets
        foreach (var (file, sheets, _) in prefetches) {
            CwlMod.Log<SourceManager>("cwl_log_workbook".Loc(file.Name));

            var migrate = MigrateDetail.GetOrAdd(file);

            WorkbookProcessor.PreProcess(migrate.Workbook!);

            foreach (var sheet in sheets) {
                var (source, _) = BySheetName(sheet, file);
                dirty.Add(source);
            }

            WorkbookProcessor.PostProcess(migrate.Workbook!);
        }

        var allocDiff = GC.GetTotalMemory(false) - alloc;
        CwlMod.Debug<WorkbookImporter>($"mem alloc: {allocDiff.ToAllocateString()}");

        return dirty.OfType<SourceData>();
    }

    public static void HotInit(IEnumerable<SourceData> sources)
    {
        CwlMod.Debug<WorkbookImporter>("resetting dirty data...");

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

        CwlMod.Debug<WorkbookImporter>("finished resetting dirty data");
    }

    private static PrefetchResult PrefetchWorkbook(FileInfo file)
    {
        var sw = Stopwatch.StartNew();

        using var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var workbook = new XSSFWorkbook(fs);
        var migrate = MigrateDetail
            .GetOrAdd(file)
            .SetWorkbook(workbook);

        List<ISheet> sheets = [];
        ISheet? element = null;

        for (var i = 0; i < workbook.NumberOfSheets; ++i) {
            var sheet = workbook.GetSheetAt(i);
            var source = FindSourceByName(sheet.SheetName);
            switch (source) {
                case SourceElement:
                    element = sheet;
                    break;
                default:
                    sheets.Add(sheet);
                    break;
                case null:
                    continue;
            }
        }

        migrate.LoadingTime += sw.ElapsedTicks;

        CwlMod.Log<SourceManager>("cwl_log_workbook_prefetch".Loc(file.ShortPath()));

        return new(file, sheets.ToArray(), element);
    }

    private sealed record PrefetchResult(FileInfo File, ISheet[] Sheets, ISheet? Element = null);
}