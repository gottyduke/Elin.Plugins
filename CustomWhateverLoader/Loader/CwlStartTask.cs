using System;
using System.Collections;
using Cwl.API.Processors;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.Unity;
using Cwl.Patches;
using Cwl.Patches.Relocation;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl;

internal sealed partial class CwlMod
{
    internal static string CurrentLoading = "";
    private static bool _duplicate;

    [Time]
    private static void BuildPatches()
    {
        _duplicate = Harmony.HasAnyPatches(ModInfo.Guid);
        if (_duplicate) {
            return;
        }

        var harmony = new Harmony(ModInfo.Guid);
        foreach (var patch in typeof(CwlMod).Assembly.DefinedTypes) {
            try {
                harmony.CreateClassProcessor(patch).Patch();
            } catch (Exception ex) {
                Error($"failed to apply patch {ex}");
                // noexcept
            }
        }

        if (CwlConfig.TrimSpaces) {
            CellPostProcessPatch.Add(c => c?.Trim());
        }
    }

    [Time]
    private static void LoadLoc()
    {
        // load CWL own localization first
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.Guid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }
    }

    private static IEnumerator LoadTask()
    {
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(CurrentLoading));

        yield return LoadDataPatch.LoadAllData();
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();

        CurrentLoading = $"[CWL] {ModInfo.Version} Has Loaded!";
    }

    private void OnStartCore()
    {
        if (_duplicate) {
            return;
        }

        QueryDeclTypes();
        GameIOProcessor.RegisterEvents();
    }

    [Time]
    private static void PrebuildDispatchers()
    {
        MethodDispatcher.BuildDispatchList<Feat>("_OnApply");
    }

    [Time]
    private static void QueryDeclTypes()
    {
        if (!CwlConfig.QualifyTypeName) {
            return;
        }

        // sources
        TypeQualifier.SafeQueryTypes<Element>();
        TypeQualifier.SafeQueryTypes<Condition>();
        TypeQualifier.SafeQueryTypes<Trait>();
        TypeQualifier.SafeQueryTypes<Quest>();
        TypeQualifier.SafeQueryTypes<Zone>();

        // extensions
        TypeQualifier.SafeQueryTypes<DramaOutcome>();
    }
}