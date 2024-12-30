using Cwl.API.Processors;
using HarmonyLib;
using SwallowExceptions.Fody;

namespace Cwl.Loader.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game), nameof(Game.Save))]
internal class GameSavePatch
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