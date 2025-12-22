using System.Text.RegularExpressions;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class AudibleTopicTextPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardRenderer), nameof(CardRenderer.Say))]
    internal static void OnExtractSoundTags(CardRenderer __instance, ref string text)
    {
        var match = Regex.Match(text, "<sound=([^>]+)>");

        if (!match.Success || match.Groups.Count <= 1) {
            return;
        }

        var soundExpr = match.Groups[1].Value.Split(',');
        var soundId = soundExpr[0];

        if (soundExpr.TryGet(1, true) is not { } chance ||
            EClass.rndf(1f) <= chance.AsFloat(1f)) {

            __instance.owner.PlaySound(soundId);
        }

        text = text.Remove(match.Index, match.Length);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Msg), nameof(Msg.SayRaw))]
    internal static void OnSayRawTaggedMsg(ref string text)
    {
        var match = Regex.Match(text, "<sound=([^>]+)>");

        if (!match.Success || match.Groups.Count <= 1) {
            return;
        }

        text = text.Remove(match.Index, match.Length);
    }
}