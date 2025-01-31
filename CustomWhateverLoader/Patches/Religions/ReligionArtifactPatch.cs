using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Religions;

[HarmonyPatch]
internal class ReligionArtifactPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Religion), nameof(Religion.IsValidArtifact))]
    internal static void OnCheckArtifact(Religion __instance, ref bool __result, string id)
    {
        if (__result) {
            return;
        }

        if (__instance is not CustomReligion custom || !EMono.sources.things.map.TryGetValue(id, out var row)) {
            return;
        }

        __result = row.tag.Contains(custom.id);
    }
}