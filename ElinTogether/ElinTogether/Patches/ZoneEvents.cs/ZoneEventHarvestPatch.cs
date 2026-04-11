using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch]
internal static class ZoneEventHarvestPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ZoneEventHarvest), nameof(ZoneEventHarvest.OnVisit))]
    internal static bool OnVisit(ZoneEventHarvest __instance)
    {
        return NetSession.Instance.IsHost;
    }
}