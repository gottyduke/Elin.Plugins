using System;
using System.IO;
using Cwl.API;
using Cwl.API.Attributes;
using Cwl.API.Migration;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
internal class SourceInitPatch
{
    internal static bool SafeToCreate = true;

    [HarmonyPrefix]
    internal static void ImportAllSheets(SourceManager __instance)
    {
        // FIXME! 23.267 stable: init called during Lang process
        __instance.initialized = false;

        // dispatch reload event
        foreach (var (sr, _) in AttributeQuery.MethodsWith<CwlSourceReloadEvent>()) {
            try {
                sr.FastInvokeStatic();
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                // noexcept
            }
        }

#if USE_BASE_FEATURE
        var imports = PackageIterator.GetAllMappings()
            .SelectMany(m => m.SourceSheets)
            .Where(f => !f.Name.StartsWith("cwl_") && !f.Name.StartsWith(".") && !f.Name.Contains("~"))
            .ToArray();

        SafeCreateSources(imports);
#endif
    }

    [HarmonyPostfix]
    internal static void OnAfterInit()
    {
        NamedImportPatch.ClearDetail();
    }

    internal static void SafeCreateSources(FileInfo[] imports)
    {
        SafeToCreate = true;

        try {
            WorkbookImporter.LoadAllFiles(imports);
            CacheDetail.FinalizeCache();
            CacheDetail.ClearDetail();
        } finally {
            SafeToCreate = false;
            CwlMod.Log<SourceManager>("cwl_log_workbook_complete".lang());
        }
    }
}