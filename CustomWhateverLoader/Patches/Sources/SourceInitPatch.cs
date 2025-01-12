using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class SourceInitPatch
{
    private const string Pattern = "*.xlsx";
    private static bool _patched;

    internal static bool SafeToCreate;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void ImportAllSheets()
    {
        if (!_patched) {
            try {
                Harmony.CreateAndPatchAll(typeof(NamedImportPatch), ModInfo.Guid);
            } catch (Exception ex) {
                CwlMod.Warn($"failed to patch Source.NamedImport, disabled\n{ex.Message.SplitNewline()[0]}");
                // noexcept
            }

            _patched = true;
        }

        SafeToCreate = true;
        try {
            var imports = PackageIterator.GetLangModFilesFromPackage()
                .SelectMany(d => d.GetFiles(Pattern, SearchOption.TopDirectoryOnly))
                .Where(f => !f.Name.Contains("cwl_migrated") && !f.Name.Contains("~$"));
            HashSet<SourceData> dirty = [EMono.sources.elements, EMono.sources.materials];

            foreach (var import in imports) {
                try {
                    CwlMod.Log("cwl_log_workbook".Loc(import.ShortPath()));

                    WorkbookImporter.BySheetName(import)
                        .OfType<SourceData>()
                        .Do(s => dirty.Add(s));
                } catch (Exception ex) {
                    CwlMod.Error("cwl_error_failure".Loc(ex));
                    // noexcept
                }
            }

            foreach (var imported in dirty) {
                try {
                    imported.Reset();
                } catch (Exception ex) {
                    CwlMod.Error("cwl_error_failure".Loc(ex));
                    // noexcept
                }
            }
        } finally {
            SafeToCreate = false;
            CwlMod.Log("cwl_log_workbook_complete".Loc());
        }
    }
}