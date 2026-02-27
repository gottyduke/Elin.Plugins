using ElinTogether.Helper;
using HarmonyLib;

[HarmonyPatch(typeof(AI_Goto), nameof(AI_Goto.TryGoTo))]
internal static class AITryGoToPatch
{
    [HarmonyPrefix]
    internal static bool OnTryGoto(AI_Goto __instance, ref AIAct.Status __result)
    {
        if (__instance.owner is not { IsRemotePlayer: true } chara) {
            return true;
        }

        if (__instance.IsDestinationReached()) {
            __result = __instance.Success();
        } else {
            __result = AIAct.Status.Running;
        }

        return false;
    }
}