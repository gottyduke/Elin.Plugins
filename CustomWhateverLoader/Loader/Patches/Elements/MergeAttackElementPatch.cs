using System.Linq;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Elements;

[HarmonyPatch]
internal class MergeAttackElementPatch
{
    [Time]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void OnInitAttackElement(SourceManager __instance)
    {
        Element.ListAttackElements.AddRange(__instance.elements.rows
            .Where(r => !Element.ListAttackElements.Contains(r))
            .Where(r => r.categorySub == "eleAttack"));
    }
}