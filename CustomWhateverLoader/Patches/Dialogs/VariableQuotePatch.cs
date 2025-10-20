﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class VariableQuotePatch
{
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
                new OperandContains(OpCodes.Callvirt, nameof(string.StartsWith)))
            .EnsureValid("startswith quotes")
            .SetInstruction(
                Transpilers.EmitDelegate(VariantStartsWith))
            .InstructionEnumeration();
    }

    [Time]
    private static bool VariantStartsWith(string lhs, string rhs)
    {
        var row = EMono.sources.langGeneral.map["_bracketTalk"];
        var quotes = new HashSet<string>(StringComparer.Ordinal) {
            "\"",
            "「",
            "“",
            rhs,
            row.text_L?.IsEmpty(rhs) ?? rhs,
        };

        return quotes.Any(lhs.StartsWith);
    }
}