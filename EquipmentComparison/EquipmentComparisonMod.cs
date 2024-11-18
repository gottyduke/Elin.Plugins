using BepInEx;
using HarmonyLib;

namespace EC;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.equipmentcomparison";
    internal const string Name = "Equipment Comparison";
    internal const string Version = "1.4";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class ECMod : BaseUnityPlugin
{
    internal static ECMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        EquipmentComparisonConfig.LoadConfig(Config);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}