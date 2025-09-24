using Cwl.Patches.Sources;
using HarmonyLib;

namespace Cwl.Patches.Things;

[HarmonyPatch]
internal class SanitizeComponentsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceThing), nameof(SourceThing.SetRow))]
    internal static void OnSetRow(SourceThing.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        SanitizeComponents(r);
    }

    private static void SanitizeComponents(SourceThing.Row r)
    {
        var components = r.components;
        for (var i = 0; i < components.Length; ++i) {
            components[i] = components[i].Replace("//", "/");
        }
    }
}