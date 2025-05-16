using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.Runtime;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class RowOverridePatch
{
    internal static bool Prepare()
    {
        return CwlConfig.OverrideSameId;
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(SourceData), nameof(SourceData.Init));
    }

    [SwallowExceptions]
    [Time]
    [HarmonyPrefix]
    internal static void TryRowOverride(SourceData __instance)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        if (__instance.GetFieldValue("rows") is not IList rows) {
            return;
        }

        var typedRows = rows.OfType<SourceData.BaseRow>().ToArray();
        if (typedRows.Length == 0) {
            return;
        }

        Dictionary<SourceData.BaseRow, SourceData.BaseRow> lastOccurrences = new(SourceRowComparer.Default);
        foreach (var row in typedRows) {
            lastOccurrences[row] = row;
        }

        List<SourceData.BaseRow> uniqueRows = new(lastOccurrences.Count);
        HashSet<SourceData.BaseRow> seen = new(SourceRowComparer.Default);

        foreach (var row in typedRows.Reverse()) {
            if (seen.Add(row)) {
                uniqueRows.Add(row);
            } else {
                CwlMod.Debug("cwl_log_deduplicate".Loc(row.GetFieldValue("id")));
            }
        }

        CwlMod.Debug("cwl_log_unique_count".Loc(__instance.GetType().Name, rows.Count, uniqueRows.Count));

        rows.Clear();
        for (var i = uniqueRows.Count - 1; i >= 0; --i) {
            rows.Add(uniqueRows[i]);
        }
    }
}