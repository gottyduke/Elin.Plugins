using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemoteAiOverride
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetAI))]
    internal static void OnSetAi(Chara __instance, ref AIAct g)
    {
        g = NetSession.Instance.Connection switch {
            // we are host
            ElinNetHost { IsConnected: true } host when host.ActiveRemoteCharas.Values.Contains(__instance) =>
                // assign all active client charas as remote
                GoalRemote.Default,
            // we are client
            ElinNetClient { IsConnected: true } when !__instance.IsPC =>
                // assign all other charas as remote
                GoalRemote.Default,
            _ => g,
        };
    }
}