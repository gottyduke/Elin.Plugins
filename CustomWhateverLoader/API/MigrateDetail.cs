using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.String;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;

namespace Cwl.API;

public sealed class MigrateDetail
{
    public enum Strategy
    {
        Correct,
        Reorder,
        Missing,
    }

    private static readonly Dictionary<IWorkbook, MigrateDetail> _cached = [];

    private MigrateSheet? CurrentSheet { get; set; }
    private string SheetFile { get; set; } = "";

    public static MigrateDetail GetOrAdd(IWorkbook book)
    {
        _cached.TryAdd(book, new());
        return _cached[book];
    }

    public MigrateDetail StartNewSheet(ISheet sheet, List<HeaderCell> expected)
    {
        CurrentSheet = new() {
            Sheet = sheet,
            Expected = expected,
        };
        return this;
    }

    public MigrateDetail SetStrategy(Strategy strategy)
    {
        if (CurrentSheet is not null) {
            CurrentSheet.MigrateStrategy = strategy;
        }

        return this;
    }

    public MigrateDetail SetGiven(List<HeaderCell> given)
    {
        if (CurrentSheet is null) {
            return this;
        }

        CurrentSheet.Given = given;
        return this;
    }

    public MigrateDetail SetFile(string filePath)
    {
        SheetFile = filePath;
        return this;
    }

    public void FinalizeMigration()
    {
        switch (CurrentSheet?.MigrateStrategy) {
            case Strategy.Reorder: {
                CwlMod.Warn("sheet has misaligned columns, CWL imported by column names instead");
                CwlMod.Warn(
                    $"if this is causing issues, please update the sheet or disable {CwlConfig.Source.NamedImport!.Definition.Key} in the config file");
                DumpHeaders();

                if (CwlConfig.Source.SheetMigrate?.Value is true) {
                    ReorderSheet();
                }

                break;
            }
            case Strategy.Missing: {
                CwlMod.Warn(
                    "sheet is missing header entries and might be incompatible, this does not prevent CWL from importing");
                DumpHeaders();
                break;
            }
        }

        CurrentSheet = null;
    }

    public void ReorderSheet()
    {
        if (CurrentSheet?.Sheet is null) {
            return;
        }

        var sheet = CurrentSheet.Sheet;
        var migratedFile = $"{SheetFile}_{sheet.SheetName}_{GameVersion.Normalized}_cwl_migrated.xlsx";
        if (File.Exists(migratedFile)) {
            CwlMod.Log(
                $"migration cancelled, sheet has already been migrated for this version: {GameVersion.Normalized}");
            return;
        }

        var book = new XSSFWorkbook();
        var migrated = book.CreateSheet(sheet.SheetName);

        try {
            var header = migrated.CreateRow(0);
            foreach (var cell in CurrentSheet.Expected) {
                var newCell = header.CreateCell(cell.Index, CellType.String);
                newCell.SetCellValue(cell.Name);
            }

            for (var i = 1; i <= sheet.LastRowNum; ++i) {
                var row = sheet.GetRow(i);
                var newRow = migrated.CreateRow(i);
                if (row is null) {
                    continue;
                }

                foreach (var (index, columnName) in CurrentSheet.Expected) {
                    var newCell = newRow.CreateCell(index, CellType.String);
                    var oldPos = CurrentSheet.Given.FindIndex(c => c.Name == columnName);
                    newCell.SetCellValue(row.Cells.ElementAtOrDefault(oldPos)?.StringCellValue ?? "");
                }
            }

            using var fs = File.OpenWrite(migratedFile);
            book.Write(fs);

            CwlMod.Log($"migration complete, sheet has been reordered at\n{migratedFile}");
        } catch (Exception ex) {
            CwlMod.Warn($"migration failure: cannot reorder sheet, this does not affect the import data\n{ex}");
            // noexcept
        }
    }

    public void DumpHeaders()
    {
        if (CurrentSheet is null) {
            return;
        }

        var expected = CurrentSheet.Expected.OrderBy(c => c.Index).ToList();
        var given = CurrentSheet.Given.OrderBy(c => c.Index).ToList();
        var maxNameWidth = expected.Max(c => c.Name.Length);

        foreach (var header in expected) {
            var expectedName = header.Name.PadRight(maxNameWidth);

            var guessCell = given.FirstOrDefault(c => c.Name == header.Name);
            var guessName = guessCell is not null && guessCell.Index != header.Index
                ? $"maybe {guessCell.Index,2}: {guessCell.Name}"
                : "";

            var givenCell = given.FirstOrDefault(c => c.Index == header.Index);
            var givenName = givenCell is not null ? givenCell.Name : "<missing>";
            givenName = givenName.PadRight(maxNameWidth + 3);

            Debug.Log($"{header.Index,2}: {expectedName} -> {givenName} {guessName}");
        }
    }

    public sealed class MigrateSheet
    {
        public List<HeaderCell> Expected = [];
        public List<HeaderCell> Given = [];
        public Strategy MigrateStrategy = Strategy.Correct;
        public ISheet? Sheet;
    }

    public sealed record HeaderCell(int Index, string Name);
}