using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class StringHelper
{
    public static ArraySegment<char> Capitalize(this ArraySegment<char> input)
    {
        ArraySegment<char> buf = input.ToArray();
        buf[0] = char.ToUpper(buf[0]);
        return buf;
    }

    public static ArraySegment<char> Capitalize(this string input)
    {
        ArraySegment<char> buf = input.ToCharArray();
        buf[0] = char.ToUpper(buf[0]);
        return buf;
    }

    public static string Truncate(this string input, int length)
    {
        return input.IsEmpty() || input.RemoveColorTag().Length <= length ? input : $"{input[..length]} ...";
    }

    public static string TruncateAllLines(this string input, int length)
    {
        return input.SplitLines().Select(s => s.Truncate(length)).Join(s => s, "\n");
    }

    public static string[] SplitLines(this string input)
    {
        return input.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    }

    public static string ToTruncateString(this object input, int length)
    {
        return input.ToString().Truncate(length);
    }

    public static string RemoveColorTag(this object input)
    {
        return Regex.Replace(input.ToString(), "<color(=[^>]*)?>|</color>", "");
    }
}