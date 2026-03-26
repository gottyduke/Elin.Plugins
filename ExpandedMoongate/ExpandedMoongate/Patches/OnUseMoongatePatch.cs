using Exm.Components;
using HarmonyLib;

namespace Exm.Patches;

[HarmonyPatch]
internal class OnUseMoongatePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitMoongate), nameof(TraitMoongate.OnUse))]
    internal static bool OnUseMoongate()
    {
        LayerExpandedMoongate.OpenPanelSesame();
        return false;
    }
}