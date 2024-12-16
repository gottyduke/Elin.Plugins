using System.Linq;
using Cwl.API;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.CustomAdv;

[HarmonyPatch]
internal class SetCharaRowPatch
{
    [Time]
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