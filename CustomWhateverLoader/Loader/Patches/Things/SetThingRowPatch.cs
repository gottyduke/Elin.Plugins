using Cwl.Loader.Patches.Sources;
using HarmonyLib;

namespace Cwl.Loader.Patches.Things;

[HarmonyPatch]
internal class SetThingRowPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SourceThing), nameof(SourceThing.SetRow))]
    internal static void OnSetRow(SourceThing.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
        }
    }
}