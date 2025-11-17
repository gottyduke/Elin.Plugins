using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Condition), nameof(Condition.Kill))]
internal static class CharaRemoveConditionEvent
{
    [HarmonyPostfix]
    internal static void OnCharaRemoveCondition(Condition __instance)
    {
        // we are not host
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        // this is more of a failsafe delta if clients couldn't clear conditions on their own
        host.Delta.AddRemote(new CharaAddConditionDelta {
            Owner = __instance.Owner,
            ConditionId = __instance.id,
            Power = 0,
            Force = true,
            Remove = true,
        });
    }
}