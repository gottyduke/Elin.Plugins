using BepInEx;
using HarmonyLib;

namespace Dona;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.donakoko";
    internal const string Name = "Donakoko Great Adventure";
    internal const string Version = "1.1.0";
}

// https://wikiwiki.jp/elonamobile/システム/仲間/仲間の評価/撮り魔『ドナココ』
[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Donakoko : BaseUnityPlugin
{
    internal static Donakoko? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        DonaConfig.Load(Config);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}