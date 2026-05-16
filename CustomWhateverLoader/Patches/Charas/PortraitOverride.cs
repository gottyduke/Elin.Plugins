using Cwl.API;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class PortraitOverride
{
    [HarmonyPatch(typeof(Chara), nameof(Chara.GetIdPortrait))]
    [HarmonyPostfix]
    internal static void OnSetPortraitOverride(Chara __instance, ref string __result)
    {
        if (__instance.mapStr.TryGetValue(CwlReservedConstants.PortraitOverride, out var portraitId) &&
            !portraitId.IsEmptyOrNull) {
            __result = portraitId;
        }
    }
}