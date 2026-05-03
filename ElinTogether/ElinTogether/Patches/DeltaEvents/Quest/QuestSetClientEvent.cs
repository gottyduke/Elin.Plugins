using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch(typeof(Quest), nameof(Quest.SetClient))]
internal static class QuestSetClientEvent
{
    [HarmonyPrefix]
    internal static void OnSetClient(Quest __instance, Chara c, bool assignQuest)
    {
        if (NetSession.Instance.Connection is not { } connection || ElinDelta.IsApplying) {
            return;
        }

        if (!EClass.game.quests.list.Contains(__instance) && __instance.chara?.quest != __instance) {
            // we can't find the quest, so just return
            return;
        }

        if (__instance.chara == c && c.quest == __instance) {
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