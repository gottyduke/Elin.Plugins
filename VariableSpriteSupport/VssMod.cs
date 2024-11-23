using BepInEx;
using HarmonyLib;

namespace VSS;

internal static class ModInfo
{
    // for legacy reason...
    internal const string Guid = "dk.elinplugins.forcepixelsize";
    internal const string Name = "Variable Sprite Support";
    internal const string Version = "1.4";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class VssMod : BaseUnityPlugin
{
    internal static VssMod? Instance { get; private set; }

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