using System;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch(typeof(DramaManager), "Update")]
internal class SafeUpdateDramaPatch
{
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    internal static void _Update(DramaManager __instance)
    {
        throw new NotImplementedException("cwl_stub");
    }

    [HarmonyPrefix]
    internal static bool SafeUpdate(DramaManager __instance)
    {
        try {
            _Update(__instance);
        } catch (Exception ex) {
            ELayerCleanup.Cleanup<LayerDrama>();

            var exp = ExceptionProfile.GetFromStackTrace(ex);
            exp.StartAnalyzing();
            exp.CreateAndPop("cwl_warn_drama_play_ex".Loc(ex.Message));
            // noexcept
        }

        return false;
    }
}