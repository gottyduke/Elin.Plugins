using System.Reflection;
using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Loader.Patches.GameIO;

[HarmonyPatch]
internal class GameSavePatch
{
    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(Game), nameof(Game.Save));
    }

    [HarmonyPrefix]
    internal static void OnPreSave(Game __instance)
    {
        GameIOProcessor.Save(__instance, false);
    }
    
    [HarmonyPostfix]
    internal static void OnPostSave(Game __instance)
    {
        GameIOProcessor.Save(__instance, true);
    }
}