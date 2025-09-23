using System;
using System.Collections;
using Cwl.API;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.API.Drama;
using Cwl.API.Migration;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches;
using Cwl.Patches.Conditions;
using Cwl.Patches.Elements;
using Cwl.Patches.Quests;
using Cwl.Patches.Relocation;
using Cwl.Patches.Sources;
using Cwl.Patches.Zones;
using HarmonyLib;
using MethodTimer;
using ReflexCLI;
using UnityEngine;

namespace Cwl;

internal sealed partial class CwlMod
{
    private static bool _loadingComplete;
    private static bool _duplicate;

    internal static string CurrentLoading
    {
        get;
        set {
            field = value;

            if (value.IsEmpty()) {
                return;
            }

            if (_loadingComplete) {
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

        foreach (var patch in typeof(CwlMod).Assembly.DefinedTypes) {
            try {
                SharedHarmony.CreateClassProcessor(patch).Patch();
            } catch (Exception ex) {
                Error<CwlMod>($"failed to patch {patch.Name}\n{ex.InnerException}");
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
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.InternalGuid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }
    }

    private IEnumerator LoadTask()
    {
        PrebuildDispatchers();
        DramaExpansion.BuildActionList();

        LoadResourcesPatch.AddHandler<SoundData>(DataLoader.RelocateSound);
        LoadResourcesPatch.AddHandler<Sprite>(DataLoader.RelocateSprite);

        DataLoader.SetupEffectTemplate();
        LoadResourcesPatch.AddHandler<Effect>(DataLoader.RelocateEffect);

        DataLoader.MergeCharaTalk();
        DataLoader.MergeCharaTone();
        DataLoader.PreloadDialog();
        DataLoader.MergeEffectSetting();

        DataLoader.LoadAllSounds();

        CustomPlaylist.RebuildBGM();
        CustomPlaylist.BuildPlaylists();

        // post init tasks
        yield return null;

        DataLoader.MergeGodTalk();
        DataLoader.MergeFactionElements();
        DataLoader.MergeOfferingMultiplier();

        CurrentLoading = $"cwl_log_finished_loading_{ModInfo.TargetVersion}".Loc();

        OnDisable();
        SetupExceptionHook();

        yield return null;

        // auto init console rebuild
        InitConsole();

        yield return null;

        ReportLoadingComplete();
    }

    private void OnStartCore()
    {
        if (_duplicate) {
            StartCoroutine(ReportDuplicateVersion());
            return;
        }

        CreateLoadingProgress();

        CacheDetail.InvalidateCache();
        QueryDeclTypes();
        RegisterEvents();

        StartCoroutine(LoadTask());
    }

    [Time]
    private static void PrebuildDispatchers()
    {
        MethodDispatcher.BuildDispatchList<Feat>("_OnApply");
        MethodDispatcher.BuildDispatchList<Trait>("_OnBarter");
        MethodDispatcher.BuildDispatchList<TraitBrewery>("_OnProduce");
    }

    [Time]
    private static void QueryDeclTypes()
    {
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
                    }
                } catch (Exception ex) {
                    Warn<CwlMod>("cwl_error_failure".Loc(ex.Message));
                    // noexcept
                }
            }
        }

        TraitTransformer.Add(CustomMerchant.TransformMerchant);
        TraitTransformer.Add(CustomConverter.TransformConverter);

        TypeResolver.Add(SafeCreateConditionPatch.ResolveCondition);
        TypeResolver.Add(SafeCreateQuestPatch.ResolveQuest);
        TypeResolver.Add(SafeCreateZonePatch.ResolveZone);

        MigrateDetail.SetupProcessor();
    }

    private static void InitConsole()
    {
        CommandRegistry.Rebuild();
        ParameterProcessorRegistry.Init();
    }

    internal static void CreateLoadingProgress()
    {
        _loadingComplete = false;

        var scrollPosition = Vector2.zero;
        ProgressIndicator
            .CreateProgress(
                () => new("cwl_log_loading".Loc(ModInfo.Version, CurrentLoading)),
                _ => _loadingComplete)
            .OnHover(_ => {
                if (!_loadingComplete) {
                    return;
                }

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400f));
                {
                    GUILayout.Label(WorkbookImporter.LastTiming);
                }
                GUILayout.EndScrollView();
            });
    }

    private static IEnumerator ReportDuplicateVersion()
    {
        yield return null;
        yield return null;
        WarnWithPopup<CwlMod>("cwl_warn_duplicate_cwl".Loc().TagColor(Color.red));
    }

    private static void ReportLoadingComplete()
    {
        _loadingComplete = true;
        Log<CwlMod>("loading complete");
    }
}