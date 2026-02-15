using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class Synchronization
{
    internal static float GameDelta;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Core), nameof(Core.Update))]
    internal static void OnCoreUpdate()
    {
        // apply remote delta happened in previous updates before this update
        switch (NetSession.Instance.Connection) {
            case ElinNetHost host:
                host.WorldStateDeltaProcess();
                return;
            case ElinNetClient client:
                GameDelta = 0;
                client.WorldStateDeltaProcess();
                return;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static void SetGameDelta()
    {
        if (NetSession.Instance.Connection is not ElinNetClient) {
            return;
        }

        Core.gameDelta = GameDelta;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static void SetTurbo()
    {
        // make remote players able to trigger turbo
        if (NetSession.Instance.Connection is not ElinNetHost || EMono.scene.paused) {
            return;
        }

        ActionMode.Adv.SetTurbo();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Core), nameof(Core.Update))]
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