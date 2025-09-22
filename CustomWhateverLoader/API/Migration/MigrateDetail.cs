using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cwl.API.Processors;
using Cwl.Helper.String;
using Cwl.LangMod;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Cwl.API.Migration;

public sealed class MigrateDetail
{
    public enum Strategy
    {
        Unknown,
        Correct,
        Reorder,
        Missing,
    }

    private static readonly string[] _emptyDefaults = [
        "Collectible",
        "KeyItem",
        "CharaText",
        "Calc",
        "Check",
        "ZoneAffix",
        "Quest",
        "Area",
        "HomeResource",
    ];

    private static readonly Dictionary<IWorkbook, MigrateDetail> _cached = [];
    private static readonly Stopwatch _sw = new();

    internal static MigrateDetail? CurrentDetail { get; private set; }

    public MigrateSheet? CurrentSheet { get; private set; }
    public BaseModPackage? Mod { get; private set; }
    public FileInfo SheetFile { get; private set; } = null!;
    public long LoadingTime { get; internal set; }

    internal static ILookup<BaseModPackage?, MigrateDetail> Details =>
        _cached
            .OrderByDescending(kv => kv.Value.LoadingTime)
            .ToLookup(kv => kv.Value.Mod, kv => kv.Value);

    public MigrateDetail StartNewSheet(ISheet sheet, Dictionary<string, int> expected)
    {
        CurrentSheet = new() {
            Sheet = sheet,
            Expected = new(expected),
        };
        return this;
    }

    public MigrateDetail SetStrategy(Strategy strategy)
    {
        CurrentSheet?.MigrateStrategy = strategy;

        return this;
    }

    public MigrateDetail SetGiven(Dictionary<string, int> given)
    {
        if (CurrentSheet is null) {
            return this;
        }

        CurrentSheet.Given = new(given);
        return this;
    }

    public MigrateDetail SetFile(FileInfo filePath)
    {
        SheetFile = filePath;
        return this;
    }

    public MigrateDetail SetMod(BaseModPackage? mod)
    {
        Mod = mod;
        return this;
    }

    public void FinalizeMigration()
    {
        switch (CurrentSheet?.MigrateStrategy) {
            case Strategy.Reorder: {
                CwlMod.Warn<MigrateDetail>("cwl_warn_misaligned_sheet".Loc(CwlConfig.Source.NamedImport!.Definition.Key));
                DumpHeaders();

                if (CwlConfig.SheetMigrate) {
                    ReorderSheet();
                }

                break;
            }
            case Strategy.Missing: {
                CwlMod.Warn("cwl_warn_missing_header".Loc());
                DumpHeaders();
                break;
            }
        }
    }

    public void ReorderSheet()
    {
        if (CurrentSheet?.Sheet is null) {
            return;
        }

        var sheet = CurrentSheet.Sheet;
        var migratedFile = $"{SheetFile}_{sheet.SheetName}_{GameVersion.Normalized}_cwl_migrated.xlsx";
        if (File.Exists(migratedFile)) {
            CwlMod.Log<MigrateDetail>("cwl_log_migration_cancel".Loc(GameVersion.Normalized));
            return;
        }

        var book = new XSSFWorkbook();
        var migrated = book.CreateSheet(sheet.SheetName);

        try {
            var header = migrated.CreateRow(0);
            foreach (var (name, index) in CurrentSheet.Expected) {
                var newCell = header.CreateCell(index, CellType.String);
                newCell.SetCellValue(name);
            }

            for (var i = 1; i <= sheet.LastRowNum; ++i) {
                var row = sheet.GetRow(i);
                var newRow = migrated.CreateRow(i);
                if (row is null) {
                    continue;
                }

                foreach (var (name, index) in CurrentSheet.Expected) {
                    var newCell = newRow.CreateCell(index, CellType.String);
                    var oldPos = CurrentSheet.Given.GetValueOrDefault(name, -1);
                    newCell.SetCellValue(row.Cells.ElementAtOrDefault(oldPos)?.StringCellValue ?? "");
                }
            }

            using var fs = File.OpenWrite(migratedFile);
            book.Write(fs);

            CwlMod.Log<MigrateDetail>("cwl_log_migration_complete".Loc(migratedFile));
        } catch (Exception ex) {
            CwlMod.Log<MigrateDetail>("cwl_warn_migration_failure".Loc(ex));
            // noexcept
        }
    }

    public void ValidateDefaults()
    {
        var sheet = CurrentSheet?.Sheet;
        if (sheet is null) {
            return;
        }

        if (_emptyDefaults.Any(s => s == sheet.SheetName)) {
            return;
        }

        var defaults = sheet.GetRow(2)?.Cells?
            .Where(c => c?.ToString() is not (null or ""));
        if (defaults?.Count() is not > 0) {
            CwlMod.Log<MigrateDetail>("cwl_warn_empty_default".Loc());
        }
    }

    public void DumpHeaders()
    {
        if (!CwlConfig.SheetInspection) {
            return;
        }

        if (CurrentSheet is null) {
            return;
        }

        CwlMod.Log<MigrateDetail>(SheetFile.ShortPath());

        var expected = CurrentSheet.Expected
            .OrderBy(c => c.Value)
            .ToList();
        var maxNameWidth = expected.Max(c => c.Key.Length);

        foreach (var (name, index) in expected) {
            var present = CurrentSheet.Given.TryGetValue(name, out var guessCell);
            var guessName = present && guessCell != index
                ? "cwl_cell_guess".Loc(guessCell, name)
                : "";
            if (!present) {
                guessName = "cwl_cell_missing".Loc();
            }

            var givenCell = CurrentSheet.Given.FirstOrDefault(c => c.Value == index);
            var givenName = givenCell.Key ?? "cwl_cell_missing".Loc();
            givenName = givenName.PadRight(maxNameWidth + 3);
            var expectedName = name.PadRight(maxNameWidth);

            CwlMod.Debug($"{index,2}: {expectedName} -> {givenName} {guessName}");
        }
    }

    public static MigrateDetail GetOrAdd(IWorkbook book)
    {
        _cached.TryAdd(book, new());
        return CurrentDetail = _cached[book];
    }

    public static void Clear()
    {
        _cached.Clear();
        CurrentDetail = null;
    }

    [SwallowExceptions]
    public static void SetupProcessor()
    {
        SheetProcessor.Add(_ => _sw.Restart(),
            false);
        SheetProcessor.Add(s => GetOrAdd(s.Workbook).LoadingTime += _sw.ElapsedTicks,
            true);

        WorkbookProcessor.Add(b => ExecutionAnalysis.MethodTimeLogger.Log(WorkbookImporter.Importer, new(GetOrAdd(b).LoadingTime), ""),
            true);
    }

    //[Conditional("DEBUG")]
    public static string DumpTiming()
    {
        var elapsed = TimeSpan.Zero;
        var total = 0;
        using var sb = StringBuilderPool.Get();
        sb.AppendLine();

        foreach (var mod in Details) {
            if (mod.Key is null) {
                continue;
            }

            var ticks = new TimeSpan(mod.Sum(d => d.LoadingTime));
            elapsed += ticks;

            var count = mod.Count();
            total += count;

            sb.AppendLine($"{(int)ticks.TotalMilliseconds,5}ms[{count,3}] {mod.Key.title}/{mod.Key.id}");
        }

        sb.AppendLine($"{(int)elapsed.TotalMilliseconds,5}ms[{total,3}] total elapsed");

        return sb.ToString();
    }

    public sealed class MigrateSheet
    {
        public Dictionary<string, int> Expected = [];
        public Dictionary<string, int> Given = [];
        public Strategy MigrateStrategy = Strategy.Unknown;
        public ISheet? Sheet;
    }
}