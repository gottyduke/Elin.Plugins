using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game), nameof(Game.Save))]
internal class GameSaveEvent
{
    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void OnPreSave()
    {
        GameIOProcessor.Save(GameIO.pathCurrentSave, false);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    internal static void OnPostSave()
    {
        GameIOProcessor.Save(GameIO.pathCurrentSave, true);
    }
}