using Cwl.Helper.Unity;
using Cwl.Loader.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.CustomElement;

[HarmonyPatch]
internal class SetElementRowPatch
{
    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceElement), nameof(SourceElement.SetRow))]
    internal static void OnSetRow(SourceElement.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        API.CustomElement.Managed[r.id] = r;

        if (SpriteReplacer.dictModItems.TryGetValue(r.alias, out var icon)) {
            SpriteSheet.Add(icon.LoadSprite(name: r.alias));
        }
    }
}