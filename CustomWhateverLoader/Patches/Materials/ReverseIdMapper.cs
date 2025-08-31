﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Materials;

[HarmonyPatch]
internal class ReverseIdMapper
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Card), nameof(Card.c_dyeMat), MethodType.Getter)]
    internal static void OnGetIdMat(Card __instance, ref int __result)
    {
        ReverseIdMap(ref __result);
        if (__result == -1) {
            __result = __instance.c_dyeMat = EClass.rnd(EMono.sources.materials.rows.Count);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Card), nameof(Card.Create))]
    internal static void OnSetIdMat(ref int _idMat)
    {
        ReverseIdMap(ref _idMat);
        if (_idMat == -1) {
            _idMat = EClass.rnd(EMono.sources.materials.rows.Count);
        }
    }

    [SwallowExceptions]
    private static void ReverseIdMap(ref int idMat)
    {
        var mat = EMono.sources.materials;
        var id = mat.rows.IndexOf(mat.map.GetValueOrDefault(idMat));
        idMat = id;
    }

    [SwallowExceptions]
    private static SourceMaterial.Row ReverseIndexer(List<SourceMaterial.Row> rows, int index)
    {
        ReverseIdMap(ref index);
        return rows.TryGet(index);
    }

    [HarmonyPatch]
    internal class RecipeMaterialIdMapper
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return [
                ..OverrideMethodComparer.FindAllOverrides(typeof(Recipe), nameof(Recipe.GetColorMaterial)),
                ..OverrideMethodComparer.FindAllOverrides(typeof(Recipe), nameof(Recipe.GetMainMaterial)),
                ..OverrideMethodComparer.FindAllOverrides(typeof(Recipe), nameof(Recipe.Build),
                    typeof(Chara), typeof(Card), typeof(Point), typeof(int), typeof(int), typeof(int), typeof(int)),
            ];
        }

        [HarmonyPrefix]
        internal static void OnGetColorId(Recipe __instance)
        {
            ReverseIdMap(ref __instance.idMat);
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> OnRowIndexerIl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new OperandMatch(OpCodes.Callvirt, o => o.ToString().Contains("List<SourceMaterial+Row>::get_Item")))
                .Repeat(cm => cm.SetInstructionAndAdvance(
                    Transpilers.EmitDelegate(ReverseIndexer))
                )
                .InstructionEnumeration();
        }
    }
}