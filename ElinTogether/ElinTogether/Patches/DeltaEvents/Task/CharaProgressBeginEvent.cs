using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.OnProgressBegin))]
internal class CharaTaskProgressEvent
{
    [HarmonyPrefix]
    internal static void OnProgressBegin(Progress_Custom __instance)
    {
        if (__instance.owner is not { } owner || owner.ai is GoalRemote) {
            return;
        }

        switch (NetSession.Instance.Connection) {
            case ElinNetHost:
                if (!owner.IsPC && owner.IsRemotePlayer) {
                    // run it only when remote players run it
                    __instance.progress = -int.MaxValue;
                }
                break;
            case ElinNetClient:
                // we can only complete remote progress with delta
                __instance.progress = -int.MaxValue;
                break;
            default:
                return;
        }

        NetSession.Instance.Connection.Delta.AddRemote(new CharaProgressDelta {
            Owner = owner,
            ActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
        });
    }
}