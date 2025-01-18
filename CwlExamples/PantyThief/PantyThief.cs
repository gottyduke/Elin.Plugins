using BepInEx;
using HarmonyLib;

namespace Panty;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.pantythief";
    internal const string Name = "Panty Thief";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Panty : BaseUnityPlugin
{
    internal static Panty? Instance { get; private set; }

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