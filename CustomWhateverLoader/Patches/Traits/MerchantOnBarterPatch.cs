using Cwl.API.Custom;
using Cwl.Helper.Runtime;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Traits;

[HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
internal class MerchantOnBarterPatch
{
    [HarmonyPrefix]
    internal static void OnSetStock(Trait __instance, out bool __state)
    {
        __state = EClass.world.date.IsExpired(__instance.owner.c_dateStockExpire);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    internal static void OnRestock(Trait __instance, bool __state)
    {
        if (!__state || __instance.owner?.id is null or "") {
            return;
        }

        if (__instance is CustomMerchant custom) {
            custom._OnBarter();
        } else {
            var externalStock = CustomMerchant.GetStockItems(__instance.owner.id);
            if (externalStock.Length > 0) {
                CustomMerchant.GenerateStock(__instance.owner, externalStock);
            }

            __instance.InstanceDispatch("_OnBarter");
        }

        FitSize(__instance);
    }

    private static void FitSize(Trait merchant)
    {
        var inv = merchant.owner?.things?.Find("chest_merchant")?.things;
        if (inv is null) {
            return;
        }

        if (inv.Count > inv.GridSize) {
            inv.ChangeSize(inv.width, Mathf.Min(inv.Count / inv.width + 1, 10));
        }
    }
}