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
        if (EClass.core.version.demo) {
            Msg.SayNothingHappen();
            return false;
        }

        LayerExpandedMoongate.OpenPanelSesame();
        return false;
    }
}