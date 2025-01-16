using System.Collections.Generic;
using HarmonyLib;

namespace Cwl.Patches.Materials;

[HarmonyPatch]
internal class ReverseIdMapper
{
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Card), nameof(Card.c_dyeMat), MethodType.Getter)]
    internal static void OnGetIdMat(Card __instance, ref int __result)
    {
        var mat = EMono.sources.materials;
        __result = mat.rows.IndexOf(mat.map.GetValueOrDefault(__result));
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Card), nameof(Card.Create))]
    internal static void OnSetIdMat(ref int _idMat)
    {
        if (_idMat == -1) {
            return;
        }

        var mat = EMono.sources.materials;
        _idMat = mat.rows.IndexOf(mat.map.GetValueOrDefault(_idMat));
    }
}