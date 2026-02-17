using System;
using System.IO;
using System.Linq;
using Cwl.API;
using Cwl.API.Attributes;
using Cwl.API.Migration;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class SourceInitPatch
{
    private static bool _patched;
    internal static bool SafeToCreate;

    internal static bool Prepare()
    {
        return !CwlMod.IsModdingApiAvailable;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void ImportAllSheets(SourceManager __instance)
    {
        // FIXME! 23.267 stable: init called during Lang process
        __instance.initialized = false;

        if (_patched) {
            // dispatch reload event
            foreach (var (sr, _) in AttributeQuery.MethodsWith<CwlSourceReloadEvent>()) {
                try {
                    sr.FastInvokeStatic();
                } catch (Exception ex) {
                    DebugThrow.Void(ex);
                    // noexcept
                }
            }
        } else {
            try {
                Harmony.CreateAndPatchAll(typeof(NamedImportPatch), ModInfo.Guid);
            } catch (Exception ex) {
                CwlMod.Warn<SourceManager>($"failed to patch Source.NamedImport, disabled\n" +
                                           $"{ex.Message.SplitLines()[0]}");
                DebugThrow.Void(ex);
                // noexcept
            }

            _patched = true;
        }

        var imports = PackageIterator.GetSourcesFromPackage()
            .Where(f => !f.Name.StartsWith("cwl_") && !f.Name.StartsWith(".") && !f.Name.Contains("~"))
            .ToArray();

        SafeCreateSources(imports);
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