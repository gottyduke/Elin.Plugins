using BepInEx;
using HarmonyLib;

namespace HRSF;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.forcepixelsize";
    internal const string Name = "High Res Sprite Fix";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class HrsfMod : BaseUnityPlugin
{
    internal static HrsfMod? Instance { get; private set; }

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