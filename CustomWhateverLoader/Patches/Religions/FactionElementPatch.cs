using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Religions;

[HarmonyPatch]
internal class FactionElementPatch
{
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThingGen), "_Create")]
    private static void TrySetCustomArtifact(ref Thing __result, string id)
    {
        if (id == "869") {
            return;
        }

        foreach (var custom in CustomReligion.All) {
            try {
                if (!custom.IsValidArtifact(id)) {
                    continue;
                }

                __result.c_idDeity = custom.id;

                foreach (var element in __result.elements.ListElements()) {
                    if (custom.IsFactionElement(element)) {
                        element.vExp = -1;
                    }
                }
            } catch {
                // noexcept
            }
        }
    }
}