using Cwl.API.Custom;
using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Patches.Religions;

internal class RefreshFaithPatch
{
    [SwallowExceptions]
    [HarmonyPostfix]
    //[HarmonyPatch(typeof(Chara), nameof(Chara.RefreshFaithElement))]
    internal static void OnRefreshFaith()
    {
        CustomReligion.SaveCustomReligion(GameIOProcessor.LastUsedContext);
    }
}