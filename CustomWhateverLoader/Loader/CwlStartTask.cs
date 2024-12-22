using System;
using System.Collections;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Loader.Patches;
using Cwl.Loader.Patches.Relocation;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader;

internal sealed partial class CwlMod
{
    private static bool _duplicate;

    [Time]
    private static void BuildPatches()
    {
        _duplicate = Harmony.HasAnyPatches(ModInfo.Guid);
        if (_duplicate) {
            return;
        }

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll(typeof(CwlForwardPatch));

        foreach (var patch in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())) {
            try {
                harmony.CreateClassProcessor(patch).Patch();
            } catch (Exception ex) {
                Error($"patch has failed: {ex.Message.SplitNewline()[0]}");
                // noexcept
            }
        }
    }

    [Time]
    private static void LoadLoc()
    {
        // load CWL own localization first
        var loc = PackageIterator.GetRelocatedFileFromPackage("cwl_sources.xlsx", ModInfo.Guid);
        if (loc is not null) {
            ModUtil.ImportExcel(loc.FullName, "General", EMono.sources.langGeneral);
        }
    }

    private static IEnumerator LoadTask()
    {
        yield return LoadDataPatch.LoadAllData();
        yield return LoadDialogPatch.LoadAllDialogs();
        yield return LoadSoundPatch.LoadAllSounds();
    }

    private void OnStartCore()
    {
        if (_duplicate) {
            return;
        }

        QueryDeclTypes();
    }

    [Time]
    private static void QueryDeclTypes()
    {
        if (!CwlConfig.QualifyTypeName) {
            return;
        }

        TypeQualifier.SafeQueryTypes<Element>();
        TypeQualifier.SafeQueryTypes<BaseCondition>();
        TypeQualifier.SafeQueryTypes<Trait>();
        TypeQualifier.SafeQueryTypes<Zone>();
    }
}