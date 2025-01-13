using BepInEx;
using HarmonyLib;
using KoC.Patches;

namespace KoC;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.karmaoncaught";
    internal const string Name = "Lose Karma On Caught";
    internal const string Version = "1.5";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal partial class KocMod : BaseUnityPlugin
{
    internal static KocMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        KocConfig.LoadConfig(Config);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
        harmony.PatchAll(typeof(OnModKarmaPatch));
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}