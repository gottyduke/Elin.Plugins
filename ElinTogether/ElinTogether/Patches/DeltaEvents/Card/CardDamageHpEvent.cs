using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.DamageHP),
    typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara))]
internal static class CardDamageHpEvent
{
    [HarmonyPrefix]
    internal static bool OnCardDamageHP()
    {
        // simply drop the update as clients and wait for delta
        return NetSession.Instance.IsHost;
    }

    extension(Card card)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_DamageHP(long dmg,
                                    int ele,
                                    int eleP,
                                    AttackSource attackSource,
                                    Card origin,
                                    bool showEffect,
                                    Thing weapon,
                                    Chara originalTarget)
        {
            throw new NotImplementedException("Chara.DamageHP");
        }
    }
}