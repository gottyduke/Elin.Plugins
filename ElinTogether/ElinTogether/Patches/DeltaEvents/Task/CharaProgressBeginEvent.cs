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

        var delta = new CharaProgressDelta {
            Owner = owner,
            ActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
        };

        switch (NetSession.Instance.Connection) {
            case ElinNetHost host:
                host.Delta.AddRemote(delta);
                if (owner.IsRemotePlayer) {
                    // run it only when remote players run it
                    __instance.progress = -int.MaxValue;
                }
                return;
            case ElinNetClient client:
                // we can only complete remote progress with delta
                __instance.progress = -int.MaxValue;
                client.Delta.AddRemote(delta);
                return;
        }
    }
}