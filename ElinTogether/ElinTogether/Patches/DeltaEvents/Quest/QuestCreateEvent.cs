using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch(typeof(Quest), nameof(Quest.Create))]
internal static class QuestCreateEvent
{
    [HarmonyPostfix]
    internal static void OnCreate(Quest __result)
    {
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        // we can't create a quest that we can't find on the client
        if (__result.person.chara.quest != __result) {
            return;
        }

        host.Delta.AddRemote(QuestCreateDelta.Create(__result));
    }
}