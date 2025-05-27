using System;
using Cwl.Helper.Runtime.Exceptions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch(typeof(DramaEventMethod), nameof(DramaEventMethod.Play))]
internal class SafePlayDramaPatch
{
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    internal static bool _Play(DramaEventMethod __instance)
    {
        throw new NotImplementedException("cwl_stub");
    }

    [HarmonyPrefix]
    internal static bool OnPlay(DramaEventMethod __instance, ref bool __result)
    {
        try {
            __result = _Play(__instance);
        } catch (Exception ex) {
            ELayerCleanup.Cleanup<LayerDrama>();

            var exp = ExceptionProfile.GetFromStackTrace(ex);
            exp.StartAnalyzing();
            exp.CreateAndPop("cwl_warn_drama_play_ex".Loc(ex.GetType().Name));
            // noexcept
        }

        return false;
    }
}