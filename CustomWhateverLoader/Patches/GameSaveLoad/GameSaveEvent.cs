using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game), nameof(Game.Save))]
internal class GameSaveEvent
{
    internal static bool Prepare()
    {
        BaseModManager.SubscribeEvent<GameIOContext>(EVENT.PostSave, ctx => GameIOProcessor.Save(GameIO.pathCurrentSave, true));
        BaseModManager.SubscribeEvent<GameIOContext>(EVENT.PreSave, ctx => GameIOProcessor.Save(GameIO.pathCurrentSave, false));

        return !CwlMod.IsModdingApiAvailable;
    }

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