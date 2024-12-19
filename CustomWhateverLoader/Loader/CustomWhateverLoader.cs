using System.Collections;
using BepInEx;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Loader.Patches;
using Cwl.Loader.Patches.Relocation;
using Cwl.Loader.Patches.Sources;
using Cwl.ThirdParty;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader;

public static class ModInfo
{
    // for legacy reason
    public const string Guid = "dk.elinplugins.customdialogloader";
    public const string Name = "Custom Whatever Loader";
    public const string Version = "1.12.5";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class CwlMod : BaseUnityPlugin
{
    internal static CwlMod? Instance { get; private set; }

    [Time]
    private void Awake()
    {
        Instance = this;

        CwlConfig.Load(Config);

        if (CwlConfig.TrimSpaces) {
            CellPostProcessPatch.Add(TrimCellProcessor.TrimCell);
        }

        // load CWL own localization first
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.Guid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll(typeof(CwlForwardPatch));
        harmony.PatchAll();
    }

    private IEnumerator Start()
    {
        Glance.TryConnect();
        yield return null;

        yield return LoadDataPatch.LoadAllData();
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();

        OnDisable();
    }

    private void OnDisable()
    {
        if (CwlConfig.Logging.Execution?.Value is true) {
            ExecutionAnalysis.DispatchAnalysis();
        }
    }
}