using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class NoApplicationPausePatch
{
    [HarmonyPostfix]
    [HarmonyCleanup]
    [HarmonyPatch(typeof(CoreConfig), nameof(CoreConfig.Apply))]
    internal static void OnOverrideBackgroundRunning()
    {
        Application.runInBackground = true;
        EMono.core.config.other.runBackground = true;
    }
}