using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class MergeAttackElementPatch
{
    [Time]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    internal static void OnInitAttackElement(SourceManager __instance)
    {
        HashSet<SourceElement.Row> existingAttackElements = new(Element.ListAttackElements);

        Element.ListAttackElements.AddRange(__instance.elements.rows
            .Where(r => r.categorySub == "eleAttack" &&
                        !existingAttackElements.Contains(r)));
    }
}