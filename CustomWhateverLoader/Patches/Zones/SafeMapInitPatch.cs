using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Zones;

//[HarmonyPatch]
internal class SafeMapInitPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EloMap), nameof(EloMap.Init))]
    internal static void OnInitCells(EloMap __instance)
    {
        __instance.region.children.ForeachReverse(z => {
            if (EMono.sources.zones.map.ContainsKey(z.id)) {
                return;
            }

            __instance.region.children.Remove(z);
            CwlMod.Log<EloMap>("cwl_warn_deserialize".Loc(nameof(Spatial), z.id, $"{z.x}/{z.y}",
                CwlConfig.Patches.SafeCreateClass.Definition.Key));
        });
    }
}