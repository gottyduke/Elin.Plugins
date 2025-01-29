using HarmonyLib;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class FixedSourceValuePatch
{
    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElementContainer), nameof(ElementContainer.ApplyElementMap))]
    internal static void OnSetFixedSourceValue(ElementContainerCard __instance, ref SourceValueType type)
    {
        var tags = __instance.owner?.sourceCard?.tag;
        if (tags is null) {
            return;
        }

        if (tags.Contains("fixedElement")) {
            type = SourceValueType.Fixed;
        } else if (tags.Contains("randomElement")) {
            type = SourceValueType.EquipmentRandom;
        }
    }
}