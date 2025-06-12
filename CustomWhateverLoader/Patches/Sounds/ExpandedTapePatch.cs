using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class ExpandedTapePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitTape), nameof(TraitTape.OnCreate))]
    internal static bool OnSetRefVal(TraitTape __instance)
    {
        var pl = EMono.Sound.currentPlaylist;
        if (pl.list.Count == 0) {
            return true;
        }

        __instance.owner.refVal = pl.list.RandomItem().data.id;
        return false;
    }
}