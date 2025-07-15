using BepInEx;
using Cwl.Helper.Unity;
using HarmonyLib;

namespace CustomizerMinus;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.charamakerminus";
    internal const string Name = "Visual PCC Picker";
    internal const string Version = "1.3.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class CmmMod : BaseUnityPlugin
{
    internal static CmmMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        CmmConfig.Load(Config);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void LogWithPopup(object payload)
    {
        Log(payload);
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(payload.ToString()));
    }
}