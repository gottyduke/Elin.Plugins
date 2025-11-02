using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Patches.Sources;
using HarmonyLib;

namespace Cwl.Patches.Elements;

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

        if (r.id is > 10000 or < 0) {
            if (r.group is nameof(FEAT) or nameof(ABILITY) or nameof(SPELL)) {
                var size = r.group switch {
                    nameof(FEAT) => 32,
                    _ => 48,
                };

                SpriteReplacerHelper.AppendSpriteSheet(r.alias, size, size);
            }
        }

        var qualified = TypeQualifier.TryQualify<Element>(r.type, r.alias);
        if (qualified?.FullName is null) {
            return;
        }

        CustomElement.AddElement(r, qualified.FullName);
    }
}