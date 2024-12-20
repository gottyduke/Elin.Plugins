using BepInEx;
using HarmonyLib;

namespace LionDance;

internal static class ModInfo
{
    // change this
    internal const string Guid = "dk.elinplugins.cwlexampleability";
    internal const string Name = "CWL Example: Custom Ability";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class LionDanceMod : BaseUnityPlugin
{
    internal static LionDanceMod? Instance { get; private set; }

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