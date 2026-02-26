using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.OnProgressBegin))]
internal static class CharaTaskProgressEvent
{
    [HarmonyPrefix]
    internal static void OnProgressBegin(Progress_Custom __instance)
    {
        if (__instance.owner is not { } owner) {
            return;
        }

        switch (NetSession.Instance.Connection) {
            case ElinNetHost:
                break;
            case ElinNetClient:
                // we can only complete remote progress with delta
                __instance.progress = -int.MaxValue;
                break;
            default:
                return;
        }

        if (owner.ai is GoalRemote) {
            // for host, run it only when remote players run it
            __instance.progress = -int.MaxValue;
            return;
        }

        NetSession.Instance.Connection.Delta.AddRemote(new CharaProgressBeginDelta {
            Owner = owner,
            Pos = owner.pos,
            MaxProgress = __instance.MaxProgress,
            ActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
        });
    }
}