using Cwl.Helper;
using Cwl.LangMod;
using Cwl.Patches.Sources;
using HarmonyLib;

namespace Cwl.Patches.Factions;

//[HarmonyPatch]
internal class SetFactionRowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceFaction), nameof(SourceFaction.SetRow))]
    internal static void OnSetRow(SourceFaction.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        var qualified = TypeQualifier.TryQualify<BaseCondition>(r.type);
        if (qualified?.FullName is null) {
            return;
        }

        if (CwlConfig.QualifyTypeName) {
            r.type = qualified.FullName;
            CwlMod.Log<SourceFaction>("cwl_log_custom_type".Loc(nameof(Faction), r.id, r.type));
        }
    }
}