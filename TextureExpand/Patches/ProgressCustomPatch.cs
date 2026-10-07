using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class ProgressCustomPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.OnProgressComplete))]
    internal static void OnProgressComplete(Progress_Custom __instance)
    {
        if (__instance.parent is AI_Shear { target: Chara chara } && RuleKeywords.Has(chara.id, Trigger.Fur)) {
            RuleState.JudgeQueue.Add(chara.uid);
        }
    }
}