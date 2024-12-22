using System.Linq;
using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Loader.Patches.Sources;
using HarmonyLib;

namespace Cwl.Loader.Patches.Elements;

[HarmonyPatch]
internal class SetElementRowPatch
{
    private static readonly string[] _managedGroups = [
        "FEAT",
        "ABILITY",
        "SPELL",
    ];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceElement), nameof(SourceElement.SetRow))]
    internal static void OnSetRow(SourceElement.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        // TODO: maybe add feat/act?
        if (_managedGroups.All(g => g != r.group)) {
            return;
        }

        if (CustomElement.Managed.ContainsKey(r.id)) {
            return;
        }

        var qualified = TypeQualifier.TryQualify<Element>(r.type, r.alias);
        if (qualified?.FullName is null) {
            return;
        }

        CustomElement.AddElement(r, qualified.FullName);
    }
}