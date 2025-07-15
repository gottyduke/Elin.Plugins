using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class StringHelper
{
    public static ReadOnlySpan<char> Capitalize(this ReadOnlySpan<char> input)
    {
        Span<char> buf = input.ToArray();
        buf[0] = char.ToUpperInvariant(buf[0]);
        return buf;
    }

    public static ReadOnlySpan<char> Capitalize(this string input)
    {
        Span<char> buf = input.ToCharArray();
        buf[0] = char.ToUpperInvariant(buf[0]);
        return buf;
    }

    public static string Truncate(this string input, int length)
    {
        return input.IsEmpty() || input.RemoveTagColor().Length <= length ? input : $"{input[..length]} ...";
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

    public static string TagColor(this object input, int hex)
    {
        var color = (uint)hex;
        var hexString = color <= 0xFFFFFF
            ? color.ToString("x6") 
            : color.ToString("x8");
        return $"<color=#{hexString}>{input}</color>";
    }

    public static string TagColorEx(this object input, Func<bool> onSwitch, 
        int good = 0x2cff14, 
        int bad = 0x000000)
    {
        return input.TagColor(onSwitch() ? good : bad);
    }

    public static StringBuilder AppendColor(this StringBuilder sb, string input, int hex)
    {
        return sb.Append(input.TagColor(hex));
    }

    public static StringBuilder AppendLineColor(this StringBuilder sb, string input, int hex)
    {
        return sb.AppendLine(input.TagColor(hex));
    }

    public static StringBuilder AppendColorEx(this StringBuilder sb, string input, Func<bool> onSwitch,
        int good = 0x2cff14,
        int bad = 0x000000)
    {
        return sb.Append(input.TagColorEx(onSwitch, good, bad));
    }

    public static StringBuilder AppendLineColorEx(this StringBuilder sb, string input, Func<bool> onSwitch,
        int good = 0x2cff14,
        int bad = 0x000000)
    {
        return sb.AppendLine(input.TagColorEx(onSwitch, good, bad));
    }

    public static string RemoveTagColor(this object input)
    {
        return Regex.Replace(input.ToString(), "<color(=[^>]*)?>|</color>", "");
    }
}