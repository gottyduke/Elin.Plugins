using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Helper.String;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Religions;

[HarmonyPatch]
internal class SetReligionRowPatch
{
    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceReligion), nameof(SourceReligion.SetRow))]
    internal static void OnSetRow(SourceReligion.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        // qualify external religion types
        if (r.type != nameof(Religion)) {
            var qualified = TypeQualifier.TryQualify<Religion>(r.type);
            r.type = qualified?.FullName ?? r.type;
            return;
        }

        var @params = r.id.Parse("#", 3);
        r.id = @params[0]!;

        if (!r.id.StartsWith("cwl_")) {
            return;
        }

        CustomReligion.GerOrAdd(r.id)
            .SetMinor(@params.Contains("minor"))
            .SetCanJoin(!@params.Contains("cannot"));
    }
}