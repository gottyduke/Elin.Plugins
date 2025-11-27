using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.LangMod;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Zones;

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
        if (qualified?.FullName is { } fullName) {
            if (CwlConfig.QualifyTypeName) {
                r.type = fullName;
                CwlMod.Log<CustomZone>("cwl_log_custom_type".Loc(nameof(Zone), r.id, r.type));
            }
        }

        CustomZone.AddZone(r);
    }
}