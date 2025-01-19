using System;
using System.Collections;
using Cwl.API.Drama;
using Cwl.API.Processors;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl;

internal sealed partial class CwlMod
{
    internal static string CurrentLoading = "";
    internal static string CanContinue = "cwl_log_loading_critical";
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

    private IEnumerator LoadTask()
    {
        using var progress = ProgressIndicator.CreateProgressScoped(
            () => new("cwl_log_loading".Loc(ModInfo.Version, CurrentLoading, CanContinue.Loc())));

        PrebuildDispatchers();
        DramaExpansion.BuildActionList();

        DataLoader.MergeCharaTalk();
        DataLoader.MergeCharaTone();
        yield return DataLoader.PreloadDialog();
        yield return DataLoader.MergeGodTalk();

        CanContinue = "";

        yield return DataLoader.LoadAllSounds();
        yield return DataLoader.MergeEffectSetting();

        CurrentLoading = "cwl_log_finished_loading".Loc();

        OnDisable();
    }

    private void OnStartCore()
    {
        if (_duplicate) {
            return;
        }

        QueryDeclTypes();
        GameIOProcessor.RegisterEvents();
        
        StartCoroutine(LoadTask());
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