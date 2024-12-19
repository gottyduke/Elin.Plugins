using Cwl.API;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Religions;

[HarmonyPatch]
internal class SetReligionOwnerPatch
{
    [Time]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReligionManager), nameof(ReligionManager.SetOwner))]
    internal static void OnSetOwner(ReligionManager __instance)
    {
        foreach (var custom in CustomReligion.All) {
            __instance.list.Add(custom);
            __instance.dictAll.Add(custom.id, custom);
            custom.Init();
        }
    }
}