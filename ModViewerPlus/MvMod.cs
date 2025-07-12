using BepInEx;
using Cwl.Helper.Unity;
using HarmonyLib;
using ViewerMinus.API;

namespace ViewerMinus;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.modviewerminus";
    internal const string Name = "Mod Viewer Plus";
    internal const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class MvMod : BaseUnityPlugin
{
    internal static MvMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        ModListManager.EnsurePath();

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void LogWithPopup(object payload)
    {
        Log(payload);
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(payload.ToString()));
    }
}