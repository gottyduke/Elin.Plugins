using System.Reflection;
using Cwl.API.Processors;
using HarmonyLib;

namespace Cwl.Loader.Patches.GameIO;

[HarmonyPatch]
internal class GameLoadPatch
{
    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(Game), nameof(Game.Load));
    }

    [HarmonyPrefix]
    internal static void OnPreLoad(Game __instance)
    {
        GameIOProcessor.Load(__instance, false);
    }
    
    [HarmonyPostfix]
    internal static void OnPostLoad(Game __instance)
    {
        GameIOProcessor.Load(__instance, true);
    }
}