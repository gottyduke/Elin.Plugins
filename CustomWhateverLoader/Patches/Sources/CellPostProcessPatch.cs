using System;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class CellPostProcessPatch
{
    internal static event CellProcess? OnCellProcess;

    internal static bool Prepare()
    {
        return CwlConfig.AllowProcessors;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExcelParser), nameof(ExcelParser.GetStr))]
    internal static string? OnGetCell(string? __result)
    {
        return OnCellProcess?.Invoke(__result);
    }

    internal static void Add(CellProcess cellProcess)
    {
        OnCellProcess += cell => {
            try {
                return cellProcess(cell);
            } catch (Exception ex) {
                CwlMod.Warn<CellProcess>("cwl_warn_processor".Loc("cell", "process", ex.Message));
                // noexcept
            }

            return cell ?? "";
        };
    }

    internal delegate string? CellProcess(string? cellValue);
}