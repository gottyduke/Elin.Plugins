

// maybe not needed
// [HarmonyPatch(typeof(Quest), nameof(Quest.Complete))]
// internal static class QuestCompleteEvent
// {
//     [HarmonyPostfix]
//     internal static void OnComplete(Quest __instance)
//     {
//         if (NetSession.Instance.Connection is not ElinNetHost host) {
//             return;
//         }

//         // host.Delta.AddRemote(new QuestCompleteDelta {
//         //     PeerUid = __instance.uid,
//         // });
//     }
// }