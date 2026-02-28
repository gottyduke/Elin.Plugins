using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Gauge), nameof(Gauge.UpdateValue), typeof(float), typeof(float))]
internal class GaugeUpdateValuePatch
{
    [HarmonyPrefix]
    internal static void OnGaugeUpdateValue(ref float now)
    {
        if (now < 0 && EClass.pc.ai.GetProgress() is { } progress) {
            now = progress.CurrentProgress + int.MaxValue;
        }
    }
}