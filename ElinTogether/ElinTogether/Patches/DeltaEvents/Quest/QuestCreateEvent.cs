using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch(typeof(Quest), nameof(Quest.Create))]
internal static class QuestCreateEvent
{
    [HarmonyPostfix]
    internal static void OnCreate(Quest __instance)
    {
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        // we can't create a quest that we can't find on the client
        if (__instance.person.chara.quest != __instance) {
            return;
        }

        host.Delta.AddRemote(QuestCreateDelta.Create(__instance));
    }
}
