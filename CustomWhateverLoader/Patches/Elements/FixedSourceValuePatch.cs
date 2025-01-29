using HarmonyLib;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class FixedSourceValuePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElementContainer), nameof(ElementContainer.ApplyElementMap))]
    internal static void OnSetFixedSourceValue(ElementContainerCard __instance, ref SourceValueType type)
    {
        if (__instance.owner.sourceCard.tag.Contains("fixedElement")) {
            type = SourceValueType.Fixed;
        } else if (__instance.owner.sourceCard.tag.Contains("randomElement")) {
            type = SourceValueType.EquipmentRandom;
        }
    }
}