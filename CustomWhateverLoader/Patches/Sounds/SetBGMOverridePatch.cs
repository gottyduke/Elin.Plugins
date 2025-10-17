using System.Collections.Generic;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class SetBGMOverridePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Zone), nameof(Zone.SetBGM), typeof(List<int>), typeof(bool), typeof(float))]
    internal static void OnSetBGMOverride(List<int> ids, bool refresh)
    {
        if (refresh && ids is [-1]) {
            Stop();
        }
    }

    private static void Stop()
    {
        var sm = SoundManager.current;
        sm.StopBGM();
        sm.SetBGMPlaylist(SoundManager.current.plBlank);
        sm.currentPlaylist = SoundManager.current.plBlank;
    }
}