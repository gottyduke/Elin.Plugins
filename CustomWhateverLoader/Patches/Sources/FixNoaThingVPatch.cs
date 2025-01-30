using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class FixNoaThingVPatch
{
    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(SourceThingV), nameof(SourceThingV.OnImportRow), 
            typeof(SourceThingV.Row),  typeof(SourceThing.Row));
    }
    
    [HarmonyPostfix]
    internal static void OnImportRow(SourceThingV.Row _r, SourceThing.Row c)
    {
        if (Lang.isBuiltin) {
            return;
        }

        c.name_L = _r.name;
        c.name2_L = _r.name2;
        c.detail_L = _r.detail;
        c.unit_L = _r.unit;
        c.unknown_L = _r.unknown;
        c.roomName_L = _r.roomName;
    }
}