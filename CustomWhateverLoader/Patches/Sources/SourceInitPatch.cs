using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API;
using Cwl.Helper.File;
using HarmonyLib;
using MethodTimer;
using NPOI.XSSF.UserModel;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class SourceInitPatch
{
    private const string Pattern = "*.xlsx";

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void ImportAllSheets()
    {
        var imports = PackageFileIterator.GetLangModFilesFromPackage()
            .SelectMany(d => d.GetFiles(Pattern, SearchOption.TopDirectoryOnly))
            .Where(f => !f.Name.Contains("cwl_migrated"));
        var sources = typeof(SourceManager)
            .GetFields(AccessTools.all)
            .Where(f => typeof(SourceData).IsAssignableFrom(f.FieldType))
            .ToList();
        HashSet<SourceData> dirty = [EMono.sources.elements, EMono.sources.materials];

        foreach (var import in imports) {
            try {
                var owner = import.Directory!.Parent!.Parent;
                var shortPath = import.FullName[(owner!.Parent!.FullName.Length + 1)..];
                CwlMod.Log($"workbook: {shortPath}");

                using var fs = File.OpenRead(import.FullName);
                var book = new XSSFWorkbook(fs);
                MigrateDetail.GetOrAdd(book).SetFile(import.GetFullFileNameWithoutExtension());

                for (var i = 0; i < book.NumberOfSheets; ++i) {
                    try {
                        var sheet = book.GetSheetAt(i);

                        var sourceField = sources.FirstOrDefault(f => f.FieldType.Name == $"Source{sheet.SheetName}" ||
                                                                      f.FieldType.Name == $"Lang{sheet.SheetName}");
                        if (sourceField is null) {
                            CwlMod.Log($"skipping sheet {import.Name}/{sheet.SheetName}");
                            continue;
                        }

                        var sheetName = $"{sourceField.Name}:{import.Name}/{sheet.SheetName}";
                        CwlMod.Log($"importing {sheetName}");

                        if (sourceField.GetValue(EMono.sources) is not SourceData source ||
                            !source.ImportData(sheet, import.Name, true)) {
                            throw new SourceParseException($"failed to import {sheetName}");
                        }

                        dirty.Add(source);
                    } catch (Exception ex) {
                        CwlMod.Error($"internal failure: {ex}");
                        // noexcept
                    }
                }
            } catch (Exception ex) {
                CwlMod.Error($"internal failure: {ex}");
                // noexcept
            }
        }

        dirty.Do(s => s.Reset());
        CwlMod.Log("finished importing workbooks");
    }
}