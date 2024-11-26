using BepInEx;
using HarmonyLib;

namespace Cwl;

internal static class ModInfo
{
    // for legacy reason
    internal const string Guid = "dk.elinplugins.customdialogloader";
    internal const string Name = "Custom Whatever Loader";
    internal const string Version = "1.1";
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

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}