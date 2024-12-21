using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Loader.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game), nameof(Game.Save))]
internal class GameSavePatch
{
    [HarmonyPrefix]
    internal static void OnPreSave()
    {
        try {
            GameIOProcessor.Save(GameIO.pathCurrentSave, false);
        } catch {
            // noexcept
        }
    }

    [HarmonyPostfix]
    internal static void OnPostSave()
    {
        try {
            GameIOProcessor.Save(GameIO.pathCurrentSave, true);
        } catch {
            // noexcept
        }
    }
}