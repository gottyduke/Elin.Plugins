﻿using System;
using System.Collections;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.API.Drama;
using Cwl.API.Processors;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches.Charas;
using Cwl.Patches.Conditions;
using Cwl.Patches.Elements;
using Cwl.Patches.Quests;
using Cwl.Patches.Recipes;
using Cwl.Patches.Relocation;
using Cwl.Patches.Sources;
using Cwl.Patches.Zones;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

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
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.Guid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }
    }

    private IEnumerator LoadTask()
    {
        using var progress = ProgressIndicator.CreateProgressScoped(() =>
            new("cwl_log_loading".Loc(ModInfo.Version, CurrentLoading, CanContinue.Loc())));

        PrebuildDispatchers();
        DramaExpansion.BuildActionList();

        LoadResourcesPatch.AddHandler<SoundData>(DataLoader.RelocateSound);
        LoadResourcesPatch.AddHandler<Sprite>(DataLoader.RelocateSprite);
        //LoadResourcesPatch.AddHandler<Effect>(DataLoader.RelocateEffect);

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

        CanContinue = "";

        CurrentLoading = "cwl_log_finished_loading".Loc();

        OnDisable();
        SetupExceptionHook();
    }

    private void OnStartCore()
    {
        if (_duplicate) {
            return;
        }

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
                }
            }
        }

        TraitTransformer.Add(CustomMerchant.TransformMerchant);
        TraitTransformer.Add(CustomConverter.TransformConverter);

        TypeResolver.Add(SafeCreateConditionPatch.ResolveCondition);
        TypeResolver.Add(SafeCreateQuestPatch.ResolveQuest);
        TypeResolver.Add(SafeCreateZonePatch.ResolveZone);
    }
}