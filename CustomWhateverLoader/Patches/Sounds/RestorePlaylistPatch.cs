using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class RestorePlaylistPatch
{
    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Zone), nameof(Zone.OnBeforeDeactivate));
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void OnInvalidateBGM(Zone __instance)
    {
        if (__instance.map is not { _plDay.Count: > 0 } map) {
            return;
        }

        for (var i = 0; i < map._plDay.Count; ++i) {
            var lookup = Core.Instance.refs.dictBGM[map._plDay[i]].name;
            if (!DataLoader.CachedSounds.ContainsKey(lookup)) {
                continue;
            }

            var reverseId = ReverseId.BGM(lookup[(lookup.LastIndexOf('/') + 1)..]);
            if (reverseId != -1 && reverseId != map._plDay[i]) {
                map._plDay[i] = reverseId;
            }
        }
    }
}