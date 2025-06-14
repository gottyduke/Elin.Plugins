﻿using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Patches.Sources;
using HarmonyLib;

namespace Cwl.Patches.Conditions;

[HarmonyPatch]
internal class SetConditionRowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceStat), nameof(SourceStat.SetRow))]
    internal static void OnSetRow(SourceStat.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        var qualified = TypeQualifier.TryQualify<BaseCondition>(r.type, r.alias);
        if (qualified?.FullName is null) {
            return;
        }

        CustomCondition.AddCondition(r, qualified.FullName);
    }
}