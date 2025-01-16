using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Patches.Objs;

[HarmonyPatch]
internal class ReverseIdMapper
{
    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(ThingGen), nameof(ThingGen.CreateObj)),
            AccessTools.Method(typeof(Map), nameof(Map.SetObj),
                [typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)]),
        ];
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void OnSetObjId(ref int id)
    {
        var objs = EMono.sources.objs;
        id = objs.rows.IndexOf(objs.map[id]);
    }
}