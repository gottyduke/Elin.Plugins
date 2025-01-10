using Cwl.API.Custom;
using Cwl.Patches.Sources;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class SetCharaRowPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SourceChara), nameof(SourceChara.SetRow))]
    internal static void OnSetRow(SourceChara.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        CustomChara.AddChara(r);
    }
}