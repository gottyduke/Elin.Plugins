using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game))]
internal class GameLoadEvent
{
    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Game.Load))]
    internal static void OnPreLoad(string id, bool cloud, out string __state)
    {
        __state = (cloud ? CorePath.RootSaveCloud : CorePath.RootSave) + id;
        GameIOProcessor.Load(__state, false);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.Load))]
    internal static void OnPostLoad(string __state)
    {
        GameIOProcessor.Load(__state, true);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.StartNewGame))]
    internal static void OnNewGame()
    {
        var path = (EClass.game.isCloud ? CorePath.RootSaveCloud : CorePath.RootSave) + Game.id;
        GameIOProcessor.Load(path, true);
    }
}