using System;
using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Religions;

[HarmonyPatch]
internal class ExtendedGiftRankPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Religion), nameof(Religion.TryGetGift))]
    internal static void OnGetGift(Religion __instance, ref bool __result)
    {
        __result = false;

        if (__instance.giftRank <= 2) {
            return;
        }

        var source = __instance.source;
        var maxRank = source.rewards.Length;
        var currentRank = __instance.giftRank;

        if (maxRank <= 2 || maxRank < currentRank) {
            return;
        }

        var pc = EClass.pc;
        var piety = pc.Evalue(ELEMENT.piety);

        if (piety < (currentRank + 1) * 15) {
            return;
        }

        try {
            var id = source.rewards[currentRank];

            if (EMono.sources.cards.map.TryGetValue(id, out var row)) {
                var pos = pc.pos.GetNearestPoint(false, false, false) ?? pc.pos;
                Card? card;

                if (row.isChara) {
                    var chara = CharaGen.Create(row.id);
                    chara.MakeAlly();
                    card = chara;
                } else {
                    var thing = ThingGen.Create(row.id, lv: pc.LV);
                    card = thing;
                }

                if (card is null) {
                    return;
                }

                EClass._zone.AddCard(card, pos);
                card.PlayEffect("aura_heaven");
            }
        } catch (Exception ex) {
            CwlMod.Warn<CustomReligion>(
                $"failed to spawn religion rewards [{currentRank}] / {source.rewards[currentRank]}\n{ex}");
            // noexcept
        }

        __instance.giftRank++;
        __result = true;
    }
}