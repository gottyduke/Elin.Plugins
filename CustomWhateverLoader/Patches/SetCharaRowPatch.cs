using System.Linq;
using Cwl.API;
using HarmonyLib;

namespace Cwl.Patches;

[HarmonyPatch]
internal class SetCharaRowPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SourceChara), nameof(SourceChara.SetRow))]
    internal static void OnSetRow(SourceChara.Row r)
    {
        var tags = r.tag
            .Where(t => t.StartsWith("addAdv"))
            .Select(t => t[6..])
            .ToArray();
        if (tags.Length != 0) {
            CustomAdventurer.AddAdventurer(r.id, tags);
        }
    }
}