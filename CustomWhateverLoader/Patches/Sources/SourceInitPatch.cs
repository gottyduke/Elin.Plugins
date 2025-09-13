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

        var imports = PackageIterator.GetSourcesFromPackage()
            .Where(f => !f.Name.StartsWith("cwl_") && !f.Name.StartsWith(".") && !f.Name.Contains("~"));

        SafeCreateSources(imports);
    }

    internal static void SafeCreateSources(IEnumerable<FileInfo> imports)
    {
        SafeToCreate = true;

        try {
            WorkbookImporter.LoadAllFiles(imports);
        } finally {
            SafeToCreate = false;
            CwlMod.Log<SourceManager>("cwl_log_workbook_complete".Loc());
        }
    }
}