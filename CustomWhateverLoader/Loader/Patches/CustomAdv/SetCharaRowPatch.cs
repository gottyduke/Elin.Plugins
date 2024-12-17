using System.Linq;
using Cwl.API;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.CustomAdv;

[HarmonyPatch]
internal class SetCharaRowPatch
{
    [Time]
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