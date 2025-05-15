using System;
using Cwl.API.Custom;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class SafeUpdateDramaPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DramaManager), "Update")]
    internal static bool SafeUpdate(DramaManager __instance)
    {
        try {
            __instance.sequence?.OnUpdate();
        } catch (Exception ex) {
            ELayerCleanup.Cleanup<LayerDrama>();
            CwlMod.ErrorWithPopup<CustomChara>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return false;
    }
}