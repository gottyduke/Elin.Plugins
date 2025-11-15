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
    internal static bool OnCardDamageHP(Card __instance,
                                        long dmg,
                                        int ele,
                                        int eleP,
                                        AttackSource attackSource,
                                        Card origin,
                                        bool showEffect,
                                        Thing weapon,
                                        Chara originalTarget)
    {
        // simply drop the update as clients and wait for delta
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        // when clients took damage, let host know
        // but we don't execute on client side
        if (connection.IsHost || __instance.IsPC) {
            connection.Delta.DeferRemote(new CardDamageHpDelta {
                Owner = __instance,
                Dmg = dmg,
                Ele = ele,
                EleP = eleP,
                AttackSource = attackSource,
                Origin = origin,
                ShowEffect = showEffect,
                Weapon = weapon,
                OriginalTarget = originalTarget,
            });
        }

        return connection.IsHost;
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