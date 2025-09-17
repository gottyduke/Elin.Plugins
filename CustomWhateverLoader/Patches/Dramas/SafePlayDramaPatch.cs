using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class SafePlayDramaPatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(Chara), nameof(Chara.ShowDialog), []),
            AccessTools.Method(typeof(DramaManager), "Update"),
            AccessTools.Method(typeof(DramaEventMethod), nameof(DramaEventMethod.Play)),
        ];
    }

    [HarmonyFinalizer]
    internal static Exception? RethrowPlayDrama(Exception? __exception)
    {
        if (__exception is null) {
            return null;
        }

        ELayerCleanup.Cleanup<LayerDrama>();

        var exp = ExceptionProfile.GetFromStackTrace(ref __exception);
        exp.StartAnalyzing();
        exp.CreateAndPop("cwl_warn_drama_play_ex".Loc($"{__exception.GetType().Name}: {__exception.Message}"));
        CwlMod.Log(__exception);

        // noexcept
        return null;
    }
}