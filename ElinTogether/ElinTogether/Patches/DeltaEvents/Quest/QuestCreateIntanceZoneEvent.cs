using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

[HarmonyPatch(typeof(QuestInstance), nameof(QuestInstance.CreateInstanceZone))]
internal static class QuestCreateInstanceZoneEvent
{
    [HarmonyPostfix]
    internal static void OnCreateInstanceZone(QuestInstance __instance, Chara c)
    {
        if (NetSession.Instance.Connection is not ElinNetClient client) {
            return;
        }

        client.Delta.AddRemote(new QuestCreateInstanceZoneDelta {
            Uid = __instance.uid,
            Chara = c,
        });
    }
}
