﻿using System.Collections;
using BepInEx;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.File;
using Cwl.Patches;
using Cwl.Patches.Relocation;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl;

public static class ModInfo
{
    // for legacy reason
    public const string Guid = "dk.elinplugins.customdialogloader";
    public const string Name = "Custom Whatever Loader";
    public const string Version = "1.10.1";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class CwlMod : BaseUnityPlugin
{
    internal static CwlMod? Instance { get; private set; }

    [Time]
    private void Awake()
    {
        Instance = this;

        CwlConfig.Load(Config);

        if (CwlConfig.Source.TrimSpaces?.Value is true) {
            PostProcessCellPatch.AddProcessor(TrimCellSpaces.TrimCell);
        }

        // load CWL own localization first
        var loc = PackageFileIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.Guid)!;
        ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    private IEnumerator Start()
    {
        DispatchGlance.TrySetupGlance();
        yield return null;

        yield return LoadDataPatch.LoadAllData();
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();

        OnDisable();
    }

    private void OnDisable()
    {
        if (CwlConfig.Logging.Execution?.Value is true) {
            ExecutionAnalysis.DispatchAnalysis();
        }
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void Debug(object payload)
    {
        if (!CwlConfig.Logging.Verbose?.Value is true) {
            return;
        }

        Instance!.Logger.LogInfo(payload);
    }

    internal static void Warn(object payload)
    {
        Instance!.Logger.LogWarning(payload);
        DispatchGlance.Dispatch(payload);
    }

    internal static void Error(object payload)
    {
        Instance!.Logger.LogError(payload);
        DispatchGlance.Dispatch(payload);
    }
}