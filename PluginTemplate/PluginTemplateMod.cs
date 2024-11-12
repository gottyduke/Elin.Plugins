using BepInEx;
using HarmonyLib;

namespace dk;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.placeholder";
    internal const string Name = "placeholder";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class PluginTemplateMod : BaseUnityPlugin
{
    internal static PluginTemplateMod? Instance { get; private set; }

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