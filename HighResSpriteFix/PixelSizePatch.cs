using HarmonyLib;

namespace HRSF;

[HarmonyPatch]
internal class PixelSizePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PCC), nameof(PCC.Build), typeof(PCCState), typeof(bool))]
    internal static void OnBuildPCC(PCC __instance)
    {
        PCCManager.current.pixelize = true;
    }
}