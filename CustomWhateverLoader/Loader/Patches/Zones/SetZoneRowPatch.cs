using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Loader.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Zones;

[HarmonyPatch]
internal class SetZoneRowPatch
{
    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceZone), nameof(SourceZone.SetRow))]
    internal static void OnSetRow(SourceZone.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        if (CustomZone.Managed.ContainsKey(r.id)) {
            return;
        }

        var qualified = TypeQualifier.TryQualify<Zone>(r.type);
        if (qualified?.FullName is null) {
            return;
        }

        CustomZone.AddZone(r, qualified.FullName);
    }
}