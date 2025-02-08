using System;
using System.Linq;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class StringHelper
{
    public static ReadOnlySpan<char> Capitalize(this ReadOnlySpan<char> input)
    {
        Span<char> buf = input.ToArray();
        buf[0] = char.ToUpper(buf[0]);
        return buf;
    }

    public static ReadOnlySpan<char> Capitalize(this string input)
    {
        Span<char> buf = input.ToCharArray();
        buf[0] = char.ToUpper(buf[0]);
        return buf;
    }

    public static string? Truncate(this string input, int length)
    {
        return input is null or "" || input.Length <= length ? input : $"{input[..length]} ...";
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
        return input.ToString().Truncate(length)!;
    }
}