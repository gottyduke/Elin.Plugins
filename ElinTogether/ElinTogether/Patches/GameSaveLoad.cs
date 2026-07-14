using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class GameSaveLoad
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.Save))]
    internal static bool OnSaveRemoteGame(ref bool __result)
    {
        if (NetSession.Instance.Connection is not ElinNetClient) {
            return true;
        }

        EmpLog.Debug("Blocked saving game with active client connection");
        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.TryLoad))]
    internal static bool OnLoadRemoteGame()
    {
        if (NetSession.Instance.Connection is not ElinNetClient || EClass.game?.player?.chara is null) {
            return true;
        }

        EmpLog.Debug("Blocked loading game with active client connection");
        return false;
    }

    [ElinPreLoad]
    internal static void TerminateConnectionOnLoad(GameIOContext context)
    {
        NetSession.Instance.RemoveComponent();
    }

    [ElinPostSceneInit]
    internal static void TerminateConnectionOnLoad(Scene.Mode mode)
    {
        if (mode == Scene.Mode.Title) {
            NetSession.Instance.RemoveComponent();
        }
    }
}