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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExcelParser), nameof(ExcelParser.GetStringArray))]
    internal static string[] OnGetCellArray(string[] __result)
    {
        for (var i = 0; i < __result.Length; ++i) {
            __result[i] = OnCellProcess?.Invoke(__result[i]) ?? __result[i];
        }
        return __result;
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