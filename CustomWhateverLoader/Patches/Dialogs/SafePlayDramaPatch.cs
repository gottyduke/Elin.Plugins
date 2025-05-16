using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.API.Custom;
using Cwl.API.Drama;
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
            CwlMod.ErrorWithPopup<CustomChara>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return false;
    }
}