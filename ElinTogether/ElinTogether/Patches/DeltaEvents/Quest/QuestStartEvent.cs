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

        // connection.Delta.AddRemote(new QuestDelta {

        // });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Quest), nameof(Quest.Create))]
    internal static void OnCreate()
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ZoneEventManager), nameof(ZoneEventManager.Add), [typeof(ZoneEvent), typeof(bool)])]
    internal static void OnAdd()
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // connection.Delta.AddRemote(new ZoneEventDelta {

        // });
    }
}