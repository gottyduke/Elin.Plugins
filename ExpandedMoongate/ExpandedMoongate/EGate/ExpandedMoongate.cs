using System.Reflection;
using BepInEx;
using Exm.Components;
using HarmonyLib;
using UnityEngine;

namespace Exm;

public static class ModInfo
{
    public const string Guid = "dk.elinplugins.expandedmoongate";
    public const string Name = "Expanded Moongate Server";
    public const string Version = "0.9.5";

    public static string BuildVersion => field ??= ExmMod.Assembly.GetName().Version.ToString();
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal partial class ExmMod : BaseUnityPlugin
{
    internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    internal static ExmMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    private void Start()
    {
        ExmConfig.InvalidateConfigs();
        ExmConfig.EnableReloadWatcher();
    }

    #if DEBUG
    private void FixedUpdate()
    {
        if (ELayer.ui.TopLayer is not (null or LayerTitle)) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.O)) {
            LayerExpandedMoongate.OpenPanelSesame();
        }
    }
    #endif
}