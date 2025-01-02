using Cwl.API.Processors;
using HarmonyLib;
using SwallowExceptions.Fody;

namespace Cwl.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game), nameof(Game.Load))]
internal class GameLoadEvent
{
    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void OnPreLoad(string id, bool cloud, out string __state)
    {
        __state = (cloud ? CorePath.RootSaveCloud : CorePath.RootSave) + id;
        GameIOProcessor.Load(__state, false);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    internal static void OnPostLoad(string __state)
    {
        GameIOProcessor.Load(__state, true);
    }
}