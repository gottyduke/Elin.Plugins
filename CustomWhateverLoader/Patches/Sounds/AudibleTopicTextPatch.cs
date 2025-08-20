using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class AudibleTopicTextPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.GetTopicText))]
    internal static IEnumerable<CodeInstruction> OnTopicTextIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Call, nameof(ClassExtension.RandomItem)),
                new(OpCodes.Ret))
            .EnsureValid("random topic text")
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryPlaySoundText))
            .InstructionEnumeration();
    }

    private static string TryPlaySoundText(string text, Chara chara)
    {
        var match = Regex.Match(text, "<sound=([^>]+)>");

        if (!match.Success || match.Groups.Count <= 1) {
            return text;
        }

        var soundExpr = match.Groups[1].Value.Split(',');
        var soundId = soundExpr[0];

        if (soundExpr.TryGet(1, true) is not { } chance ||
            EClass.rndf(1f) <= chance.AsFloat(1f)) {
            chara.PlaySound(soundId);
        }

        return text.Remove(match.Index, match.Length);
    }
}