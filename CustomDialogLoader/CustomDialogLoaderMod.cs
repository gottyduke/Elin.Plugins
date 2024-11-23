using BepInEx;
using HarmonyLib;

namespace Cdl;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.customdialogloader";
    internal const string Name = "Custom Dialog Loader";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class CdlMod : BaseUnityPlugin
{
    internal static CdlMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}