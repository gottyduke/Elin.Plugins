using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Dialogs;

[HarmonyPatch]
internal class VariableQuotePatch
{
    private static List<string>? _variantQuotes;

    internal static bool Prepare()
    {
        return CwlConfig.VariableQuote;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.TalkTopic))]
    internal static IEnumerable<CodeInstruction> OnCompareQuoteIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(string),
                    nameof(string.StartsWith),
                    [typeof(string)])),
                new CodeMatch(OpCodes.Brtrue))
            .Advance(-1)
            .SetInstruction(
                Transpilers.EmitDelegate(VariantStartsWith))
            .InstructionEnumeration();
    }

    [Time]
    private static bool VariantStartsWith(string lhs, string rhs)
    {
        var row = EMono.sources.langGeneral.map["_bracketTalk"];
        _variantQuotes ??= [
            "\"",
            "「",
            row.text,
            row.text_JP,
        ];

        return lhs.StartsWith(rhs) ||
               _variantQuotes.Any(lhs.StartsWith) ||
               (row.text_L is not "" && lhs.StartsWith(row.text_L));
    }
}