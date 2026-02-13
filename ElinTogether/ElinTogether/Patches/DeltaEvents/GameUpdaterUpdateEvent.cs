using System.Diagnostics;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(GameUpdater), nameof(GameUpdater.Update))]
internal static class GameUpdaterUpdateEvent
{
    internal static int AllowedUpdate = 0;
    [HarmonyPrefix]
    internal static void OnGameUpdaterUpdate()
    {
        EClass.scene.paused |= ActionModeCombat.Paused;
        if (EClass.scene.paused) {
            return;
        }

        switch (NetSession.Instance.Connection) {
            case ElinNetHost host:
                host.Delta.AddRemote(new GameUpdateDelta());
                return;
            case ElinNetClient:
                if (AllowedUpdate > 0) {
                    AllowedUpdate--;
                } else {
                    EClass.scene.paused = true;
                }

                return;
        }
    }
}