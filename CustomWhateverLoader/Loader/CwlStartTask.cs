using System.Collections;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Loader.Patches;
using Cwl.Loader.Patches.Relocation;
using HarmonyLib;

namespace Cwl.Loader;

internal sealed partial class CwlMod
{
    private static void BuildPatches()
    {
        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll(typeof(CwlForwardPatch));
        harmony.PatchAll();
    }

    private static void LoadLoc()
    {
        // load CWL own localization first
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.Guid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }
    }

    private IEnumerator LoadTask()
    {
        yield return LoadDataPatch.LoadAllData();
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();
    }

    private void OnStartCore()
    {
        TypeQualifier.SafeQueryTypes();
    }
}