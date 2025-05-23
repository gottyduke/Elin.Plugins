﻿using System.Reflection;
using BepInEx;
using HarmonyLib;
using ReflexCLI;

namespace ACS;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.animatedcustomsprites";
    internal const string Name = "Animated Custom Sprites";
    internal const string Version = "1.8";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class AcsMod : BaseUnityPlugin
{
    internal static AcsMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
        CommandRegistry.assemblies.Add(Assembly.GetExecutingAssembly());
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void Warn(object payload)
    {
        Instance!.Logger.LogWarning(payload);
    }

    internal static void Error(object payload)
    {
        Instance!.Logger.LogError(payload);
    }
}