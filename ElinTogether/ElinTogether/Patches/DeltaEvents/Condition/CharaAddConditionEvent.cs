using System;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.AddCondition), typeof(Condition), typeof(bool))]
internal static class CharaAddConditionEvent
{
    [HarmonyPostfix]
    internal static void OnCharaAddCondition(Chara __instance, Condition? __result, Condition c, bool force)
    {
        // only propagate successful add condition events
        if (__result is null) {
            return;
        }

        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        host.Delta.AddRemote(new CharaAddConditionDelta {
            Owner = __instance,
            ConditionId = __result.id,
            Power = __result.power,
            Force = force,
        });
    }

    [HarmonyPrefix]
    internal static bool OnClientAddCondition()
    {
        // clients cannot add conditions normally
        return NetSession.Instance.IsHost;
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal Condition Stub_AddCondition(Condition condition, bool force)
        {
            throw new NotImplementedException("Chara.AddCondition");
        }
    }
}