using System.Collections;
using BepInEx;
using Cwl.Patches;
using HarmonyLib;

namespace Cwl;

internal static class ModInfo
{
    // for legacy reason
    internal const string Guid = "dk.elinplugins.customdialogloader";
    internal const string Name = "Custom Whatever Loader";
    internal const string Version = "1.7";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class CwlMod : BaseUnityPlugin
{
    internal static CwlMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();
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