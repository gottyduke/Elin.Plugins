using Cwl.API.Custom;
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

        var @params = r.id.Parse("#", 3);
        r.id = @params[0];

        if (!r.id.Contains("cwl_")) {
            return;
        }

        CustomReligion.GerOrAdd(r.id)
            .SetMinor(@params.Contains("minor"))
            .SetCanJoin(!@params.Contains("cannot"));
    }
}