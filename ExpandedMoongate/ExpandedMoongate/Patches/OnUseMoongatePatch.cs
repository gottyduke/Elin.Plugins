using EGate.Components;
using HarmonyLib;

namespace EGate.Patches;

[HarmonyPatch]
internal class OnUseMoongatePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitMoongate), nameof(TraitMoongate.OnUse))]
    internal static bool OnUseMoongate()
    {
        if (EClass.core.version.demo) {
            Msg.SayNothingHappen();
            return false;
        }

        LayerExpandedMoongate.OpenPanelSesame();
        return false;
    }
}