using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Loader.Patches.GameSaveLoad;

[HarmonyPatch(typeof(Game), nameof(Game.Load))]
internal class GameLoadPatch
{
    [HarmonyPrefix]
    internal static void OnPreLoad(string id, bool cloud, out string __state)
    {
        __state = (cloud ? CorePath.RootSaveCloud : CorePath.RootSave) + id;
        try {
            GameIOProcessor.Load(__state, false);
        } catch {
            // noexcept
        }
    }

    [HarmonyPostfix]
    internal static void OnPostLoad(string __state)
    {
        try {
            GameIOProcessor.Load(__state, true);
        } catch {
            // noexcept
        }
    }
}