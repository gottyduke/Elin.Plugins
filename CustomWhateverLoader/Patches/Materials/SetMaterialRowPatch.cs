using System;
using System.Linq;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches.Sources;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl.Patches.Materials;

[HarmonyPatch]
internal class SetMaterialRowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceMaterial), nameof(SourceMaterial.SetRow))]
    internal static void OnSetRow(SourceMaterial.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        AddMaterial(r);
    }

    [Time]
    private static void AddMaterial(SourceMaterial.Row r)
    {
        var matColors = Core.Instance.Colors.matColors;
        if (matColors.ContainsKey(r.alias)) {
            return;
        }

        Color main = default;
        Color alt = default;

        var tags = r.tag
            .Where(t => t.StartsWith("addCol"))
            .ToArray();

        try {
            foreach (var tag in tags) {
                var value = tag.ExtractInBetween('(', ')');
                if (value.IsEmptyOrNull) {
                    continue;
                }

                if (tag.StartsWith("addCol_Main")) {
                    main = value.ToColorEx();
                } else if (tag.StartsWith("addCol_Alt")) {
                    alt = value.ToColorEx();
                }
            }
        } catch (Exception ex) {
            CwlMod.WarnWithPopup<SourceMaterial>("cwl_warn_mat_color".Loc(r.alias, string.Join(", ", tags), ex.Message), ex);
            return;
        }

        matColors[r.alias] = new() {
            main = main,
            alt = alt,
        };
        CwlMod.Log<SourceMaterial>("cwl_log_mat_color".Loc(r.alias, main, alt));
    }
}