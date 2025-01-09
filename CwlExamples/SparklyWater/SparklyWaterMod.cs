using BepInEx;
using Cwl.API.Processors;
using Cwl.Helper;
using HarmonyLib;
using Sparkly.Traits;

namespace Sparkly;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.sparklywater";
    internal const string Name = "Sparkly Water For All";
    internal const string Version = "1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Sparkly : BaseUnityPlugin
{
    internal static Sparkly? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    private void Start()
    {
        TraitTransformer.Add(TraitSparklyWater.TransformBooze);

        ref var categories = ref EMono.sources.categories.map;
        var booze = categories["booze"];
        var sparkly = categories["sparkly"];

        var id = booze.id;
        var uid = booze.uid;

        sparkly.IntrospectCopyTo(booze);
        booze.id = id;
        booze.uid = uid;
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
}