using System;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Loader.Patches.Sources;

public class CellPostProcessPatch
{
    public delegate string? CellProcess(string? cellValue);

    private static event CellProcess OnCellProcess = cell => cell;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExcelParser), nameof(ExcelParser.GetStr))]
    internal static void OnGetCell(ref string? __result)
    {
        __result = OnCellProcess(__result);
    }

    public static void Add(CellProcess cellProcess)
    {
        OnCellProcess += cell => {
            try {
                return cellProcess(cell);
            } catch (Exception ex) {
                CwlMod.Warn("cwl_cell_post_process".Loc(ex));
                // noexcept
            }

            return cell;
        };
    }
}