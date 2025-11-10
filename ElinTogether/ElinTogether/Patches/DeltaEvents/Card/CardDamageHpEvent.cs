using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.DeltaEvents;

[HarmonyPatch(typeof(Card), nameof(Card.DamageHP),
    typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara))]
internal static class CardDamageHpEvent
{
    [HarmonyPrefix]
    internal static bool OnCardDamageHP(Card __instance,
                                        long dmg,
                                        int ele,
                                        int eleP = 100,
                                        AttackSource attackSource = AttackSource.None,
                                        Card? origin = null,
                                        bool showEffect = true,
                                        Thing? weapon = null,
                                        Chara? originalTarget = null)
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost { IsConnected: true } host:
                host.Delta.AddRemote(new CardDamageHpDelta {
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
                return true;
            case ElinNetClient { IsConnected: true } client:
                // we are clients, drop the update and wait for delta
                client.Delta.AddRemote(new CardDamageHpDelta {
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
                return false;
            default:
                return true;
        }
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