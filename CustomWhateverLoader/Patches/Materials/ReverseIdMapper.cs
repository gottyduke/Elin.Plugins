using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Materials;

[HarmonyPatch]
internal class ReverseIdMapper
{
    [SwallowExceptions]
    private static SourceMaterial.Row ReverseIndexer(List<SourceMaterial.Row> rows, int index)
    {
        var mat = EMono.sources.materials;
        return mat.map.GetValueOrDefault(index) ?? mat.map[MATERIAL.granite];
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            ..OverrideMethodComparer.FindAllOverrides(typeof(Recipe), nameof(Recipe.GetColorMaterial)),
            ..OverrideMethodComparer.FindAllOverrides(typeof(Recipe), nameof(Recipe.GetMainMaterial)),
            ..OverrideMethodComparer.FindAllOverrides(typeof(Recipe), nameof(Recipe.Build),
                typeof(Chara), typeof(Card), typeof(Point), typeof(int), typeof(int), typeof(int), typeof(int)),
            AccessTools.DeclaredMethod(typeof(TraitBoat), nameof(TraitBoat.GetWaterMat)),
            AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Create)),
            AccessTools.PropertyGetter(typeof(Card), nameof(Card.DyeMat)),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnRowIndexerIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandMatch(OpCodes.Callvirt, o => o.ToString().Contains("List<SourceMaterial+Row>::get_Item")))
            .Repeat(cm => cm
                .SetInstructionAndAdvance(
                    Transpilers.EmitDelegate(ReverseIndexer)))
            .InstructionEnumeration();
    }

    [HarmonyPatch]
    internal class SerializedCardDyeMatIdMapper
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SerializedCards), nameof(SerializedCards.Restore))]
        internal static IEnumerable<CodeInstruction> OnRowTryGetIl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new OperandContains(OpCodes.Callvirt, "Dye(Row)"))
                .EnsureValid("serialized card restore idDyeMat")
                .Advance(-2)
                .RemoveInstructions(2)
                .InsertAndAdvance(
                    Transpilers.EmitDelegate(ReverseIndexer))
                .InstructionEnumeration();
        }
    }
}