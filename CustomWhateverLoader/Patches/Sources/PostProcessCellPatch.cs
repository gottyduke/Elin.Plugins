using System;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class PostProcessCellPatch
{
    public delegate string? CellProcessor(string? cellValue);

    public static event CellProcessor OnCellProcess = cell => cell;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExcelParser), nameof(ExcelParser.GetStr))]
    internal static void OnGetCell(ref string? __result)
    {
        __result = OnCellProcess(__result);
    }

    internal static void AddProcessor(CellProcessor cellProcessor)
    {
        OnCellProcess += cell => {
            try {
                return cellProcessor(cell);
            } catch (Exception ex) {
                CwlMod.Warn("cwl_cell_post_process".Loc(ex));
                // noexcept
            }

            return cell;
        };
    }
}