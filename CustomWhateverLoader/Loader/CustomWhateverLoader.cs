using System.Collections;
using BepInEx;
using Cwl.Helper;
using Cwl.Loader.Patches.Sources;
using Cwl.ThirdParty;

namespace Cwl.Loader;

public static class ModInfo
{
    // for legacy reason
    public const string Guid = "dk.elinplugins.customdialogloader";
    public const string Name = "Custom Whatever Loader";
    public const string Version = "1.13.11";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class CwlMod : BaseUnityPlugin
{
    internal static CwlMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        CwlConfig.Load(Config);

        if (CwlConfig.TrimSpaces) {
            CellPostProcessPatch.Add(TrimCellProcessor.TrimCell);
        }

        LoadLoc();
        BuildPatches();
    }

    private IEnumerator Start()
    {
        if (_duplicate) {
            yield break;
        }
        
        Glance.TryConnect();

        yield return null;
        yield return LoadTask();

        OnDisable();
    }

    private void OnDisable()
    {
        if (CwlConfig.Logging.Execution?.Value is true) {
            ExecutionAnalysis.DispatchAnalysis();
        }
    }
}