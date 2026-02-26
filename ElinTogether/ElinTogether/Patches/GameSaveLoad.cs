using Cwl.API.Attributes;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class GameSaveLoad
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameIO), nameof(GameIO.SaveGame))]
    internal static bool OnSaveRemoteGame()
    {
        if (NetSession.Instance.Connection is not ElinNetClient) {
            return true;
        }

        EmpLog.Debug("Blocked saving game with active client connection");
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.TryLoad))]
    internal static bool OnLoadRemoteGame()
    {
        if (NetSession.Instance.Connection is not ElinNetClient) {
            return true;
        }

        EmpLog.Debug("Blocked loading game with active client connection");
        return false;
    }

    [CwlPreLoad]
    [CwlSceneInitEvent(Scene.Mode.Title, preInit: true)]
    internal static void TerminateConnectionOnLoad()
    {
        NetSession.Instance.RemoveComponent();
    }
}