using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class ConBoostPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ConBoost), nameof(ConBoost.GetRendererReplacer))]
    internal static bool OnGetRendererReplacer(ConBoost __instance, ref RendererReplacer? __result)
    {
        if (!RuleKeywords.IsAnyTarget(__instance.owner.id)) {
            return true;
        }

        __result = null;
        return false;
    }
}