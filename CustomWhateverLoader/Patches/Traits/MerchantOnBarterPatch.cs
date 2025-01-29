using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Traits;

[HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
internal class MerchantOnBarterPatch
{
    [HarmonyPrefix]
    internal static void OnSetStock(Trait __instance, out bool __state)
    {
        __state = __instance is CustomMerchant merchant &&
                  EClass.world.date.IsExpired(merchant.owner.c_dateStockExpire);
    }

    [HarmonyPostfix]
    private static void ShouldGenerate(Trait __instance, bool __state)
    {
        if (__instance is CustomMerchant merchant && __state) {
            merchant.Generate();
        }
    }
}