using System;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class AudibleTopicTextPatch
{
    private const string SoundTagPrefix = "<sound=";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardRenderer), nameof(CardRenderer.Say))]
    internal static void OnExtractSoundTags(CardRenderer __instance, ref string text)
    {
        while (TryExtractSoundTag(ref text, out var soundId, out var chance)) {
            if (EClass.rndf(1f) <= chance) {
                __instance.owner.PlaySound(soundId);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Msg), nameof(Msg.SayRaw))]
    internal static void OnSayRawTaggedMsg(ref string text)
    {
        TryExtractSoundTag(ref text, out _, out _);
    }

    private static bool TryExtractSoundTag(ref string text, out string? id, out float chance)
    {
        id = null;
        chance = 1f;

        var start = text.IndexOf(SoundTagPrefix, StringComparison.Ordinal);
        if (start == -1) {
            return false;
        }

        var end = text.IndexOf('>', start);
        if (end == -1) {
            return false;
        }

        var soundId = start + SoundTagPrefix.Length;
        var expr = text.Substring(soundId, end - soundId);

        var comma = expr.IndexOf(',');
        if (comma == -1) {
            id = expr;
        } else {
            id = expr[..comma];
            var chanceStr = expr[(comma + 1)..];

            if (float.TryParse(chanceStr, out var c)) {
                chance = c;
            }
        }

        text = text.Remove(start, end - start + 1);
        return true;
    }
}