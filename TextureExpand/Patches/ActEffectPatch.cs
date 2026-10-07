using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class ActEffectPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActEffect), nameof(ActEffect.Proc), typeof(EffectId), typeof(int), typeof(BlessedState), typeof(Card),
        typeof(Card), typeof(ActRef))]
    internal static void OnProc(EffectId id, Card cc, Card? tc)
    {
        if (id != EffectId.TransGender || (tc ?? cc) is not Chara chara || !RuleKeywords.Has(chara.id, Trigger.Gender)) {
            return;
        }

        RuleState.JudgeQueue.Add(chara.uid);
    }
}