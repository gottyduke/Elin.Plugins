using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
internal static class CharaDieEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaDie(Chara __instance, Element? e, Card? origin, AttackSource attackSource, Chara? originalTarget)
    {
        switch (NetSession.Instance.Connection) {
            case ElinNetHost host:
                host.Delta.AddRemote(new CharaDieDelta {
                    Owner = __instance,
                    ElementId = e?.id,
                    Origin = origin,
                    AttackSource = attackSource,
                    OriginalTarget = originalTarget,
                });
                return true;
            case ElinNetClient:
                // we are clients, drop the update and wait for delta
                return false;
            default:
                return true;
        }
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal void Stub_Die(Element e, Card origin, AttackSource attackSource, Chara originalTarget)
        {
            throw new NotImplementedException("Chara.Die");
        }
    }
}