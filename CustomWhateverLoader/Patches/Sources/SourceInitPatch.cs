using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                CwlMod.Warn<SourceManager>($"failed to patch Source.NamedImport, disabled\n" +
                                           $"{ex.Message.SplitNewline()[0]}");
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

            // init elements and materials first so other sheets can parse it properly
            HotInit(dirty);

            foreach (var import in imports) {
                try {
                    CwlMod.Log<SourceManager>("cwl_log_workbook".Loc(import.ShortPath()));

                    dirty.UnionWith(WorkbookImporter.BySheetName(import).OfType<SourceData>());
                } catch (Exception ex) {
                    CwlMod.Error<SourceManager>("cwl_error_failure".Loc(ex));
                    // noexcept
                }
            }

            HotInit(dirty);
        } finally {
            SafeToCreate = false;
            CwlMod.Log<SourceManager>("cwl_log_workbook_complete".Loc());
        }

#if DEBUG
        var elapsed = 0L;
        var total = 0;
        var sb = new StringBuilder(2048);
        sb.AppendLine();

        foreach (var mod in MigrateDetail.Details) {
            if (mod.Key is null) {
                continue;
            }

            var time = mod.Sum(d => d.LoadingTime);
            elapsed += time;

            var count = mod.Count();
            total += count;

            sb.AppendLine($"{time,5}ms[{count,3}] {mod.Key.title}/{mod.Key.id}");
        }

        sb.AppendLine($"{elapsed}ms[{total,3}] total elapsed");
        CwlMod.Log<MigrateDetail>(sb);
#endif

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
                CwlMod.Error<SourceManager>("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }
    }
}