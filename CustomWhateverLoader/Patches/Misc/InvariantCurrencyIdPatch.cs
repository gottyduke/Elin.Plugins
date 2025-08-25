using HarmonyLib;

namespace Cwl.Patches.Misc;

[HarmonyPatch]
internal class InvariantCurrencyIdPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InvOwner.Transaction), nameof(InvOwner.Transaction.IDCurrency), MethodType.Getter)]
    internal static bool OnGetLowerCaseId(InvOwner.Transaction __instance, ref string __result)
    {
        __result = __instance.currency.ToString().ToLowerInvariant();
        return false;
    }
}