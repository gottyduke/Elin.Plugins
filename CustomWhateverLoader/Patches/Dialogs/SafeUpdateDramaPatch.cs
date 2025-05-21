using System;
using Cwl.API.Custom;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

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
            CwlMod.ErrorWithPopup<CustomChara>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return false;
    }
}