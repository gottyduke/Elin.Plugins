using System.Collections;
using BepInEx;
using Cwl.Helper;
using Cwl.Patches;
using Cwl.Patches.Relocation;
using HarmonyLib;
using MethodTimer;

namespace Cwl;

internal static class ModInfo
{
    // for legacy reason
    internal const string Guid = "dk.elinplugins.customdialogloader";
    internal const string Name = "Custom Whatever Loader";
    internal const string Version = "1.9";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class CwlMod : BaseUnityPlugin
{
    internal static CwlMod? Instance { get; private set; }

    [Time]
    private void Awake()
    {
        Instance = this;

        CwlConfig.Load(Config);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();
    }

    private void OnDestroy()
    {
        if (CwlConfig.Logging.Execution?.Value is true) {
            ExecutionAnalysis.DispatchAnalysis();
        }
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void Debug(object payload)
    {
        if (!CwlConfig.Logging.Verbose?.Value is true) {
            return;
        }

        Instance!.Logger.LogInfo(payload);
    }

    internal static void Warn(object payload)
    {
        Instance!.Logger.LogWarning(payload);
    }

    internal static void Error(object payload)
    {
        Instance!.Logger.LogError(payload);
    }
}