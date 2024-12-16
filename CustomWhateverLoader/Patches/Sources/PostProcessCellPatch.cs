using System;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class PostProcessCellPatch
{
    public delegate string CellProcessor(string cellValue);
    public static event CellProcessor OnCellProcess = cell => cell;

    [Time]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExcelParser), nameof(ExcelParser.GetStr))]
    internal static void OnGetCell(ref string __result)
    {
        __result = OnCellProcess(__result);
    }
    
    internal static void AddProcessor(CellProcessor cellProcessor)
    {
        OnCellProcess += cell => {
            try {
                return cellProcessor(cell);
            } catch (Exception ex) {
                CwlMod.Warn($"cell post processor failed, this does not affect importing\n{ex}");
                // noexcept
            }

            return cell;
        };
    }
}