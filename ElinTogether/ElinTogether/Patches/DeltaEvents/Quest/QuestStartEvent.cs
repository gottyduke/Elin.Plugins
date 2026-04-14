using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class QuestStartEvent
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuestManager), nameof(QuestManager.Start), typeof(Quest))]
    internal static void OnStart(Quest q)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // already started
        if (EClass.game.quests.list.Contains(q)) {
            return;
        }

        var owner = q.person.chara;
        var canFind = owner?.quest == q;
        connection.Delta.AddRemote(new QuestStartDelta {
            Uid = q.uid,
            Owner = owner,
            Data = canFind ? null : LZ4Bytes.Create(q),
        });
    }
}