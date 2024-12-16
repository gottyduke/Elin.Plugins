using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API;
using Cwl.Helper.File;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class SourceInitPatch
{
    private const string Pattern = "*.xlsx";

    internal static bool SafeToCreate;

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void ImportAllSheets()
    {
        var imports = PackageFileIterator.GetLangModFilesFromPackage()
            .SelectMany(d => d.GetFiles(Pattern, SearchOption.TopDirectoryOnly))
            .Where(f => !f.Name.Contains("cwl_migrated"))
            .ToList();

        HashSet<SourceData> dirty = [EMono.sources.elements, EMono.sources.materials];

        foreach (var import in imports) {
            try {
                CwlMod.Log("cwl_log_workbook".Loc(import.ShortPath()));

                WorkbookImporter
                    .BySheetName(import)
                    .OfType<SourceData>()
                    .Do(s => dirty.Add(s));
            } catch (Exception ex) {
                CwlMod.Error("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }

        SafeToCreate = true;
        dirty.Do(s => s.Reset());
        SafeToCreate = false;

        CwlMod.Log("cwl_log_workbook_complete".Loc());
    }
}