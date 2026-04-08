using System.Linq;
using ACS.API;
using HarmonyLib;

namespace ACS.Patches;

[HarmonyPatch]
internal class ReloadSuffixPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpriteReplacer), nameof(SpriteReplacer.Reload))]
    internal static void OnReloadSuffixes(SpriteReplacer __instance, string id)
    {
        if (__instance.data is null || __instance.suffixes.Count == 0) {
            return;
        }

        var suffixes = __instance.suffixes
            .Where(kv => kv.Key.StartsWith("_acs_"))
            .ToArray();
        if (suffixes.Length == 0) {
            return;
        }

        foreach (var (suffix, data) in suffixes) {
            var clip = AcsClip.CreateFromFormat(suffix);
            if (clip is { Length: > 0 }) {
                data.frame = clip.Length;
                data.time = clip.Interval / 1000f;
                data.scale = 100;

                __instance.suffixes[$"_acs_{clip.Name}"] = data;
                __instance.suffixes[AcsController.ReservedSuffix] = null;

                AcsMod.Log($"loaded '{id}' clip '{clip.Name}' with {clip.Length} frames @ {clip.Interval}ms");
            }

            __instance.suffixes.Remove(suffix);
        }
    }
}