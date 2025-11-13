using System;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.UseAbility), typeof(Act), typeof(Card), typeof(Point), typeof(bool))]
internal static class CharaUseAbilityEvent
{
    [HarmonyPostfix]
    internal static void OnCharaUseAbility(Chara __instance, Act a, Card? tc, Point? pos, bool pt)
    {
        if (NetSession.Instance.Connection is not { }  connection) {
            return;
        }

        // always propagate use ability deltas to remote
        // difference is that clients will have all subroutines blocked
        connection.Delta.AddRemote(new CharaUseAbilityDelta {
            Owner = __instance,
            ActId = a.id,
            TargetCard = tc,
            Pos = pos,
            Party = pt,
        });
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal bool Stub_UseAbility(Act act, Card? tc = null, Point? pos = null, bool pt = false)
        {
            throw new NotImplementedException("Chara.UseAbility");
        }
    }
}