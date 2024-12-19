using System;
using System.Linq;
using HarmonyLib;

namespace Cwl.Helper;

public class TrimCellProcessor
{
    public static string? TrimCell(string? cell)
    {
        return cell?.SplitNewline()
            .Select(s => s.Trim())
            .Where(s => s is not "")
            .Join(s => s, Environment.NewLine);
    }
}