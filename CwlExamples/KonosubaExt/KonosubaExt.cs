using BepInEx;
using Cwl.API.Processors;
using HarmonyLib;
using KonoExt.Traits;

namespace KonoExt;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.konosubaext";
    internal const string Name = "Konosuba Character Addon";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class KonoExt : BaseUnityPlugin
{
    internal static KonoExt? Instance { get; private set; }

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

    internal void OnStartCore()
    {
        TraitTransformer.Add(TraitKonoAdv.TransformKono);
    }
}