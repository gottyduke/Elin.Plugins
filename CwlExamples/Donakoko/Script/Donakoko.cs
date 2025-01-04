using BepInEx;
using Cwl.API.Processors;
using Dona.Patches;
using HarmonyLib;

namespace Dona;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.donakoko";
    internal const string Name = "Donakoko Great Adventure";
    internal const string Version = "1.0.0";
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

    private void OnStartCore()
    {
        GameIOProcessor.AddLoad(PostLoadEvent.AddItemIfMissing, true);
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}