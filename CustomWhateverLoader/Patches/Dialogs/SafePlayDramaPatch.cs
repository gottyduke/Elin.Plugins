using System;
using System.Reflection;
using Cwl.API.Custom;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class SafePlayDramaPatch
{
    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(DramaEventMethod), nameof(DramaEventMethod.Play));
    }

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