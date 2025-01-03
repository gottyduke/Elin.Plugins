using Cwl.API.Custom;
using Cwl.Helper;
using HarmonyLib;
using MethodTimer;
using SwallowExceptions.Fody;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class FeatApplyEvent
{
    [Time]
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Feat), nameof(Feat.Apply))]
    internal static void OnApply(Feat __instance, int a, ElementContainer owner, bool hint)
    {
        if (!CustomElement.Managed.ContainsKey(__instance.id)) {
            return;
        }

        __instance.InstanceDispatch("_OnApply", a, owner, hint);
    }
}