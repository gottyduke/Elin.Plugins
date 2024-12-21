using System.Linq;
using Cwl.API.Custom;
using Cwl.Loader.Patches.Sources;
using HarmonyLib;

namespace Cwl.Loader.Patches.Adv;

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

        var tags = r.tag
            .Where(t => t.StartsWith("addAdv"))
            .Select(t => t[6..])
            .ToArray();
        if (tags.Length != 0) {
            CustomAdventurer.AddAdventurer(r.id, tags);
        }
    }
}