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

        connection.Delta.AddRemote(new QuestStartDelta {
            Uid = q.uid,
            RefChara = q.person.chara,
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Quest), nameof(Quest.Create))]
    internal static void OnCreate()
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        //
    }

    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(QuestInstance), nameof(QuestInstance.CreateInstanceZone))]
    // internal static void OnCreateInstanceZone(Chara c)
    // {
    //     if (NetSession.Instance.Connection is not { } connection) {
    //         return;
    //     }

    //     connection.Delta.AddRemote(new ZoneEventQuestCreateDelta {
    //         RefChara = c,
    //     });
    // }
}