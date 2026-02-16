using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class Synchronization
{
    internal static float GameDelta { get; set; }

    [HarmonyPatch(typeof(Core), nameof(Core.Update))]
    internal static class CoreSynchronizationContext
    {
        [HarmonyPrefix]
        internal static void OnCoreUpdate()
        {
            // apply remote delta happened in previous updates before this update
            switch (NetSession.Instance.Connection) {
                case ElinNetHost host:
                    host.WorldStateDeltaProcess();
                    return;
                case ElinNetClient client:
                    GameDelta = 0f;
                    client.WorldStateDeltaProcess();
                    return;
            }
        }

        [HarmonyPostfix]
        internal static void OnCoreUpdateEnd()
        {
            switch (NetSession.Instance.Connection) {
                case ElinNetHost host:
                    if (!EMono.scene.paused) {
                        host.Delta.AddRemote(new GameDelta {
                            Delta = Core.gameDelta,
                        });
                    }

                    host.WorldStateDeltaUpdate();
                    return;
                case ElinNetClient client:
                    client.WorldStateDeltaUpdate();
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static class GameSynchronizationContext
    {
        [HarmonyPrefix]
        internal static void SetGameDelta()
        {
            switch (NetSession.Instance.Connection) {
                // apply game delta as clients
                case ElinNetClient:
                    Core.gameDelta = GameDelta;
                    break;
                // allow remote players to trigger turbo
                case ElinNetHost when !EMono.scene.paused:
                    ActionMode.Adv.SetTurbo();
                    break;
            }
        }
    }
}