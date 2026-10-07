using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class ZonePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Zone), nameof(Zone.OnVisit))]
    internal static void OnVisit(Zone __instance)
    {
        if (!RuleRegistry.HasRules) {
            return;
        }

        foreach (var chara in __instance.map.charas) {
            if (RuleKeywords.IsAnyTarget(chara.id)) {
                RuleState.JudgeQueue.Add(chara.uid);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone), nameof(Zone.AddCard), typeof(Card), typeof(int), typeof(int))]
    internal static void OnAddCard(Card t)
    {
        if (RuleKeywords.IsAnyTarget(t.id)) {
            RuleState.JudgeQueue.Add(t.uid);
        }
    }
}