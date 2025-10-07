using HarmonyLib;

namespace Cwl.Patches.Races;

[HarmonyPatch]
internal class SafeGetRaceRowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceChara.Row), nameof(SourceChara.Row.race_row), MethodType.Getter)]
    internal static void OnSafeGetRaceRow(SourceChara.Row __instance)
    {
        if (EMono.sources.races.map.ContainsKey(__instance.race)) {
            return;
        }

        __instance.race = "norland";
        CwlMod.Warn<SourceRace>($"replaced invalid race {__instance.race} on {__instance.id}");
    }
}