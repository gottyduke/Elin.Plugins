using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class CharaBodyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharaBody), nameof(CharaBody.Unequip), typeof(BodySlot), typeof(bool))]
    internal static void OnUnequip(CharaBody __instance, bool refresh)
    {
        if (refresh) {
            QueueEquip(__instance.owner);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharaBody), nameof(CharaBody.Equip), typeof(Thing), typeof(BodySlot), typeof(bool))]
    internal static void OnEquip(CharaBody __instance, bool msg)
    {
        if (msg) {
            QueueEquip(__instance.owner);
        }
    }

    private static void QueueEquip(Chara owner)
    {
        if (owner.isCreated && EClass.core.IsGameStarted && RuleKeywords.Has(owner.id, Trigger.Equip)) {
            RuleState.JudgeQueue.Add(owner.uid);
        }
    }
}