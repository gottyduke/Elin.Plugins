using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper;
using HarmonyLib;
using NPOI.XSSF.UserModel;

namespace Cwl.Patches;

[HarmonyPatch]
internal class ImportPatch
{
    private const string Pattern = "*.xlsx";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void ImportAllSheets()
    {
        var langs = PackageFileIterator.GetLangModFilesFromPackage();
        var imports = langs.SelectMany(d => d.GetFiles(Pattern, SearchOption.TopDirectoryOnly));
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

                var book = new XSSFWorkbook(import);
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
                            throw new($"failed to import {sheetName}");
                        }

                        dirty.Add(source);
                    } catch (Exception ex) {
                        CwlMod.Error($"internal failure: {ex.Message}");
                        // noexcept
                    }
                }
            } catch (Exception ex) {
                CwlMod.Error($"internal failure: {ex.Message}");
                // noexcept
            }
        }

        dirty.Do(s => s.Reset());
        CwlMod.Log("finished importing workbooks");
    }
}