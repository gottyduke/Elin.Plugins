using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Effects;

[HarmonyPatch]
internal class CaneTintPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttackProcess), nameof(AttackProcess.PlayRangedAnime))]
    internal static IEnumerable<CodeInstruction> OnSetEffColorIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, nameof(SourceElement.Row.alias)),
                new OperandContains(OpCodes.Callvirt, nameof(Color)),
                new OperandContains(OpCodes.Stfld, "effColor"))
            .ThrowIfInvalid("failed to match effColor indexer")
            .Advance(-1)
            .RemoveInstruction()
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryOverrideTint))
            .InstructionEnumeration();
    }

    private static Color TryOverrideTint(UD_String_Color colors, string alias, AttackProcess process)
    {
        if (!colors.TryGetValue(alias, out var mainColor)) {
            mainColor = colors[alias] = colors.FirstItem();
            CwlMod.Warn<ColorProfile>($"missing color: {alias}, using fallback");
        }

        if (!DataLoader.CachedEffectData.TryGetValue(process.weapon.id, out var effect) ||
            !colors.TryGetValue(process.weapon.id, out var @override)) {
            return mainColor;
        }

        return effect.caneColorBlend
            ? Color.Lerp(mainColor, @override, 0.5f)
            : @override;
    }
}