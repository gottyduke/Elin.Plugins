using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Sources;

[HarmonyPatch]
internal class SafeCreateCardPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SafeCreateCardPatch), "CreateCard")]
    internal static IEnumerable<CodeInstruction> OnAddRowIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Call, AccessTools.Method(
                    typeof(SourceCard),
                    nameof(SourceCard.AddRow))))
            .Repeat(cm => cm
                .SetInstruction(Transpilers.EmitDelegate(RethrowCreateInvoke)))
            .InstructionEnumeration();
    }

    [Time]
    private static void RethrowCreateInvoke(SourceCard card, CardRow row, bool isChara)
    {
        try {
            card.AddRow(row, isChara);
        } catch (Exception ex) {
            CwlMod.Warn("cwl_warn_card_create".Loc(row.GetType().Name, row.id, row.name, ex));
            // noexcept
        }
    }
}