using BepInEx;
using HarmonyLib;

namespace PL;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.fixedpackageloader";
    internal const string Name = "Fixed Package Loader";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class FixedPackageLoader : BaseUnityPlugin
{
    private static FixedPackageLoader? Instance { get; set; }
    private void Awake()
    {
        Instance = this;

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance?.Logger.LogInfo(payload);
    }
}