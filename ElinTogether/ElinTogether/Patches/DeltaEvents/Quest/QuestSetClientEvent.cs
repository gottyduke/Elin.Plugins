using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch(typeof(Quest), nameof(Quest.SetClient))]
internal static class QuestSetClientEvent
{
    [HarmonyPrefix]
    internal static void OnSetClient(Quest __instance, Chara c, bool assignQuest)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        if (!EClass.game.quests.list.Contains(__instance) && __instance.person.chara?.quest != __instance) {
            // we can't find the quest, so just return
            return;
        }

        connection.Delta.AddRemote(new QuestSetClientDelta {
            Uid = __instance.uid,
            Owner = __instance.person.chara,
            NewChara = c,
            AssignQuest = assignQuest,
        });
    }
}
