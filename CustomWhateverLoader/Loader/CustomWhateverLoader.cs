using System.Collections;
using BepInEx;
using Cwl.API.Drama;
using Cwl.Helper;

namespace Cwl;

public static class ModInfo
{
    // for legacy reason
    public const string Guid = "dk.elinplugins.customdialogloader";
    public const string Name = "Custom Whatever Loader";
    public const string Version = "1.18.9";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class CwlMod : BaseUnityPlugin
{
    internal static CwlMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        CwlConfig.Load(Config);

        LoadLoc();
        BuildPatches();
    }

    private IEnumerator Start()
    {
        if (_duplicate) {
            yield break;
        }

        PrebuildDispatchers();
        DramaExpansion.BuildActionList();

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