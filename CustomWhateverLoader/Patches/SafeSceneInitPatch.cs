using Cwl.API;
using Cwl.Helper;
using HarmonyLib;

namespace Cwl.Patches;

// credits to 105gun
[HarmonyPatch]
internal class SafeSceneInitPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene), nameof(Scene.Init))]
    internal static void OnSceneInit(Scene.Mode newMode)
    {
        if (newMode != Scene.Mode.StartGame) {
            return;
        }

        CustomAdventurer.SafeToCreate = true;
        CoroutineHelper.Immediate(CustomAdventurer.AddDelayedChara());
        CoroutineHelper.Deferred(
            () => CustomAdventurer.SafeToCreate = false,
            () => EMono.core is null || EMono.game is null);
    }
}