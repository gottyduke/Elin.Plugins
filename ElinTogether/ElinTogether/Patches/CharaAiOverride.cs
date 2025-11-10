using ElinTogether.Elements;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class CharaAiOverride
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetAI))]
    internal static void OnSetAi(Chara __instance, ref AIAct g)
    {
        g = NetSession.Instance.Connection switch {
            // we are host, assign all active client charas as remote
            ElinNetHost { IsConnected: true } host
                when host.ActiveRemoteCharas.Contains(__instance) => GoalRemote.Default,
            // we are client, assign all other charas as remote
            ElinNetClient { IsConnected: true } when !__instance.IsPC => GoalRemote.Default,
            _ => g,
        };
    }
}