using System.Reflection;
using BepInEx;
using EGate.Components;
using HarmonyLib;
using UnityEngine;

namespace EGate;

public static class ModInfo
{
    public const string Guid = "dk.elinplugins.expandedmoongate";
    public const string Name = "Expanded Moongate Server";
    public const string Version = "0.9.5";

    public static string BuildVersion => field ??= EgMod.Assembly.GetName().Version.ToString();
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal partial class EgMod : BaseUnityPlugin
{
    internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    internal static EgMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    private void Start()
    {
        EgConfig.InvalidateConfigs();
        EgConfig.EnableReloadWatcher();
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