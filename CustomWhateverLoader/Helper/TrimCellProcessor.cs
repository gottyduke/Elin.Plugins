using System;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Helper;

public class TrimCellProcessor
{
    [Time]
    public static string? TrimCell(string? cell)
    {
        return cell?.SplitNewline().Join(s => s.Trim(), Environment.NewLine);
    }
}