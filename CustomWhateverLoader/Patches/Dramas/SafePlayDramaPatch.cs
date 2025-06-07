using System;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class SafePlayDramaPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(DramaEventMethod), nameof(DramaEventMethod.Play))]
    internal static Exception? OnPlay(DramaEventMethod __instance, Exception? __exception)
    {
        if (__exception is null) {
            return null;
        }

        ELayerCleanup.Cleanup<LayerDrama>();

        var exp = ExceptionProfile.GetFromStackTrace(__exception);
        exp.StartAnalyzing();
        exp.CreateAndPop("cwl_warn_drama_play_ex".Loc(__exception.Message));

        // noexcept
        return null;
    }
}