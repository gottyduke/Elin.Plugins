using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl.Patches.Materials;

[HarmonyPatch]
internal class SafeCreateMaterialPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [Time]
    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElementContainer), nameof(ElementContainer.ApplyMaterialElementMap))]
    internal static void SafeApplyInvoke(ElementContainer __instance, Thing t)
    {
        var map = EMono.sources.materials.map;
        if (map.ContainsKey(t.idMaterial)) {
            return;
        }

        map[t.idMaterial] = map[1];

        CwlMod.WarnWithPopup<Material>("cwl_warn_deserialize".Loc(nameof(SourceMaterial), t.idMaterial,
            nameof(SourceMaterial.Row),
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));
    }
}