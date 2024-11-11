using BepInEx;
using HarmonyLib;

namespace KoC;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.karmaoncaught";
    internal const string Name = "Lose Karma On Caught";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class KarmaOnCaughtMod : BaseUnityPlugin
{
    internal static KarmaOnCaughtMod? Instance { get; private set; }

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