using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch]
internal static class QuestCreateEvent
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone), nameof(Zone.UpdateQuests))]
    internal static bool OnUpdateQuests()
    {
        return NetSession.Instance.IsHost;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Quest), nameof(Quest.Create))]
    internal static void OnCreate(Quest __result)
    {
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        // we can't create a quest that we can't find on the client
        if (__result.person.chara?.quest != __result) {
            return;
        }

        host.Delta.AddRemote(QuestCreateDelta.Create(__result));
    }
}