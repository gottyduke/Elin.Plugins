using Cwl.API;
using Cwl.Helper;
using Cwl.Loader.Patches.Sources;
using HarmonyLib;

namespace Cwl.Loader.Patches.Elements;

[HarmonyPatch]
internal class SetElementRowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceElement), nameof(SourceElement.SetRow))]
    internal static void OnSetRow(SourceElement.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        // TODO: maybe add feat/act?
        if (r.group is not ("ABILITY" or "SPELL") || r.type is "") {
            return;
        }

        if (CustomElement.Managed.ContainsKey(r.id)) {
            return;
        }

        var qualified = TypeQualifier.TryQualify<Act>(r.type);
        if (qualified?.FullName is null) {
            return;
        }

        CustomElement.AddElement(r, qualified.FullName);
    }
}