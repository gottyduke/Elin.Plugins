using System;
using HarmonyLib;

namespace Cwl.Helper;

public class TrimCellSpaces
{
    public static string TrimCell(string cell)
    {
        return cell.SplitNewline().Join(s => s.Trim(), Environment.NewLine);
    }
}