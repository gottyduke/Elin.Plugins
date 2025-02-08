using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API;
using Cwl.API.Migration;
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
                CwlMod.WarnWithPopup<SourceManager>($"failed to patch Source.NamedImport, disabled\n" +
                                                    $"{ex.Message.SplitLines()[0]}", ex);
                // noexcept
            }

            _patched = true;
        }

        SafeToCreate = true;
        try {
            var imports = PackageIterator.GetLangModFilesFromPackage()
                .SelectMany(d => d.GetFiles(Pattern, SearchOption.TopDirectoryOnly))
                .Where(f => !f.Name.StartsWith("cwl_") && !f.Name.Contains("~$"));
            HashSet<SourceData> dirty = [EMono.sources.elements, EMono.sources.materials];

            // init elements and materials first so other sheets can parse it properly
            HotInit(dirty);

            foreach (var import in imports) {
                try {
                    CwlMod.Log<SourceManager>("cwl_log_workbook".Loc(import.ShortPath()));

                    dirty.UnionWith(WorkbookImporter.BySheetName(import).OfType<SourceData>());
                } catch (Exception ex) {
                    CwlMod.WarnWithPopup<SourceManager>("cwl_error_failure".Loc(ex.Message), ex);
                    // noexcept
                }
            }

            HotInit(dirty);
        } finally {
            SafeToCreate = false;
            CwlMod.Log<SourceManager>("cwl_log_workbook_complete".Loc());
        }

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
                CwlMod.ErrorWithPopup<SourceManager>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }
}