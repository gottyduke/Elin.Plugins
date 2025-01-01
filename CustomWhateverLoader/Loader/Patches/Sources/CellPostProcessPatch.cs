using System;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class CellPostProcessPatch
{
    private static event CellProcess OnCellProcess = cell => cell;

    internal static bool Prepare()
    {
        return CwlConfig.AllowProcessors;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExcelParser), nameof(ExcelParser.GetStr))]
    internal static void OnGetCell(ref string? __result)
    {
        __result = OnCellProcess(__result);
    }

    internal static void Add(CellProcess cellProcess)
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

    internal delegate string? CellProcess(string? cellValue);
}