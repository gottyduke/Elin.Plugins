using System;
using System.Collections;
using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.API.Drama;
using Cwl.API.Migration;
using Cwl.API.Processors;
using Cwl.Components;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches;
using Cwl.Patches.Charas;
using Cwl.Patches.Conditions;
using Cwl.Patches.Elements;
using Cwl.Patches.Materials;
using Cwl.Patches.Quests;
using Cwl.Patches.Relocation;
using Cwl.Patches.Sources;
using Cwl.Patches.Traits;
using Cwl.Patches.Zones;
using Cwl.Scripting;
using HarmonyLib;
using MethodTimer;
using ReflexCLI;
using ReflexCLI.UI;
using UnityEngine;

namespace Cwl;

internal sealed partial class CwlMod
{
    private static bool _duplicate;

    internal static bool LoadingComplete { get; private set; }

    internal static string CurrentLoading
    {
        get;
        set {
            field = value;

            if (value.IsEmptyOrNull) {
                return;
            }

            if (LoadingComplete) {
                CreateLoadingProgress();
            }
        }
    } = "";

    [Time]
    private static void BuildPatches()
    {
        _duplicate = Harmony.HasAnyPatches(ModInfo.Guid);
        if (_duplicate) {
            return;
        }

        BuildPatchesFromTypes(typeof(CwlMod).Assembly.DefinedTypes);

        if (CwlConfig.TrimSpaces) {
            CellPostProcessPatch.Add(c => c?.Trim());
        }
    }

    [Time]
    private static void BuildDeferredPatches()
    {
        List<Type> deferred = [
            typeof(ActPerformEvent),
            typeof(ConverterEvent.CanDecaySubEvent),
            typeof(ConverterEvent.OnDecaySubEvent),
            typeof(LoadZonePatch),
            typeof(RepositionTcPatch.TcFixPosPatch),
            typeof(InvalidateItemPatch),
            typeof(InvalidateDestThingPatch),
            typeof(ReverseIdMapper.RecipeMaterialIdMapper),
        ];

        BuildPatchesFromTypes(deferred, true);
    }

    private static void BuildPatchesFromTypes(IEnumerable<Type> types, bool deferred = false)
    {
        foreach (var def in types) {
            try {
                SharedHarmony.CreateClassProcessor(def, deferred).Patch();
            } catch (Exception ex) {
                Error<CwlMod>($"failed to apply {def.Name}\n{ex.InnerException}");
                // noexcept
            }
        }
    }

    [Time]
    private static void LoadLoc()
    {
        // load CWL own localization first
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.InternalGuid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }
    }

    private static IEnumerator LoadTask()
    {
        DataLoader.RefreshAllPackageTextures();

        PrebuildDispatchers();
        DramaExpansion.BuildActionList();

        AddResourceRelocators();

        DataLoader.PreloadDialog();
        DataLoader.MergeEffectSetting();

        AddSoundsAndBGM();

        // post init tasks
        yield return null;

        DataLoader.MergeGodTalk();
        DataLoader.MergeCharaTalk();
        DataLoader.MergeCharaTone();
        //DataLoader.MergeCustomAlias();
        //DataLoader.MergeCustomName();

        DataLoader.MergeFactionElements();
        DataLoader.MergeOfferingMultiplier();

        CurrentLoading = $"cwl_log_finished_loading_{ModInfo.TargetVersion}".lang();

        yield return null;

        // auto init console rebuild
        InitConsole();
        ContextMenuHelper.AddDelayedContextMenu();

        ExecutionAnalysis.DispatchAnalysis();

        ReportLoadingComplete();
    }

    private void OnStartCore()
    {
        if (_duplicate) {
            StartCoroutine(ReportDuplicateVersion());
            CwlReloadTask.Unload();
            return;
        }

        CwlConfig.Watch(Config);

        SetupExceptionHook();

        CreateLoadingProgress();

        CacheDetail.InvalidateCache();

        InitRuntimeFeatures();

        BuildDeferredPatches();

        RegisterEvents();

        StartCoroutine(LoadTask());
    }

    private static void AddResourceRelocators()
    {
        LoadResourcesPatch.AddHandler<SoundData>(DataLoader.RelocateSound);
        LoadResourcesPatch.AddHandler<Sprite>(DataLoader.RelocateSprite);

        DataLoader.SetupEffectTemplate();
        LoadResourcesPatch.AddHandler<Effect>(DataLoader.RelocateEffect);
    }

    private static void AddSoundsAndBGM()
    {
        DataLoader.LoadAllSounds();

        CustomPlaylist.RebuildBGM();
        CustomPlaylist.BuildPlaylists();
    }

    [Time]
    private static void PrebuildDispatchers()
    {
        MethodDispatcher.BuildDispatchList<Feat>("_OnApply");
        MethodDispatcher.BuildDispatchList<Trait>("_OnBarter");
        MethodDispatcher.BuildDispatchList<TraitBrewery>("_OnProduce");
    }

    [Time]
    private static void InitRuntimeFeatures()
    {
        // scripts
        CwlScriptLoader.LoadAllPackageScripts();

        if (!CwlConfig.DefaultScriptState.IsEmptyOrNull) {
            CwlScriptLoader.PushState(CwlConfig.DefaultScriptState);
        }

        // sources
        TypeQualifier.SafeQueryTypesOfAll();
    }

    [Time]
    private static void RegisterEvents()
    {
        foreach (var (method, attrs) in AttributeQuery.MethodsWith<CwlEvent>()) {
            foreach (var attr in attrs) {
                try {
                    switch (attr) {
                        case CwlGameIOEvent ioAttr:
                            GameIOProcessor.RegisterEvents(method, ioAttr);
                            break;
                        case CwlActPerformEvent actAttr:
                            ActPerformEvent.RegisterEvents(method, actAttr);
                            break;
                        case CwlContextMenu ctxAttr:
                            ContextMenuHelper.RegisterEvents(method, ctxAttr);
                            break;
                        case CwlOnCreateEvent charaAttr:
                            CardOnCreateEvent.RegisterEvents(method, charaAttr);
                            break;
                        case CwlSceneInitEvent sceneInitAttr:
                            SafeSceneInitEvent.RegisterEvents(method, sceneInitAttr);
                            break;
                        case CwlDramaAction dmAttr:
                            DramaExpansion.RegisterEvents(method, dmAttr);
                            break;
                    }
                } catch (Exception ex) {
                    Warn<CwlMod>("cwl_error_failure".Loc(ex.Message));
                    // noexcept
                }
            }
        }

        GameIOProcessor.RegisterContextVars();

        TraitTransformer.Add(CustomMerchant.TransformMerchant);
        TraitTransformer.Add(CustomConverter.TransformConverter);

        TypeResolver.Add(SafeCreateConditionPatch.ResolveCondition);
        TypeResolver.Add(SafeCreateQuestPatch.ResolveQuest);
        TypeResolver.Add(SafeCreateZonePatch.ResolveZone);
        TypeResolver.Add(CustomReligion.ResolveReligion);

        MigrateDetail.SetupProcessor();
    }

    private static void InitConsole()
    {
        CommandRegistry.Rebuild();
        ParameterProcessorRegistry.Init();

        var console = ReflexConsole.Instance;
        console.ui = Instantiate(Resources.Load<ReflexUIManager>("ReflexConsoleCanvas"));
        console.ui.input.HistoryContainer.AddItem("Logo", console.logo.text);

        if (CwlConfig.AllowScripting) {
            console.ui.input.HistoryContainer.AddItem("cwl.cs.is_ready", "cwl_ui_cs_ready".lang());
        }

        Instance.GetOrCreate<CwlConsole>();
        Instance.GetOrCreate<CwlPipe>();
    }

    private static void CreateLoadingProgress()
    {
        LoadingComplete = false;

        ProgressIndicator
            .CreateProgress(
                () => new("cwl_log_loading".Loc(ModInfo.Version, CurrentLoading)),
                _ => LoadingComplete);
    }

    private static IEnumerator ReportDuplicateVersion()
    {
        yield return null;
        yield return null;
        WarnWithPopup<CwlMod>("cwl_warn_duplicate_cwl".lang().TagColor(Color.red));
    }

    private static void ReportLoadingComplete()
    {
        LoadingComplete = true;
        Log<CwlMod>("loading complete");
    }
}