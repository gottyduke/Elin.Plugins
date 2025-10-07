using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class StringHelper
{
    public static string ToAllocateString(this long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        var suffix = 0;
        var readable = bytes;

        while (readable >= 1L << 10 && suffix < suffixes.Length - 1) {
            readable >>= 10;
            suffix++;
        }

        double fmt;
        if (suffix == 0) {
            fmt = bytes;
        } else {
            var divisor = 1L << (suffix * 10);
            fmt = (double)bytes / divisor;
        }

        return $"{fmt:0.##} {suffixes[suffix]}";
    }

    extension(string input)
    {
        public string Capitalize()
        {
            Span<char> buf = input.ToCharArray();
            buf[0] = char.ToUpperInvariant(buf[0]);
            return buf.ToString();
        }

        public string Truncate(int length)
        {
            return input.IsEmpty() || input.RemoveTagColor().Length <= length ? input : $"{input[..length]} ...";
        }

        public string TruncateAllLines(int length)
        {
            return input.SplitLines().Select(s => s.Truncate(length)).Join(s => s, "\n");
        }

        public string[] SplitLines()
        {
            return input.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
    }

    extension(object input)
    {
        public string ToTruncateString(int length)
        {
            return input.ToString().Truncate(length);
        }

        public string TagColor(int hex)
        {
            var color = (uint)hex;
            var hexString = color <= 0xFFFFFF
                ? color.ToString("x6")
                : color.ToString("x8");
            return $"<color=#{hexString}>{input}</color>";
        }

        public string TagColorEx(Func<bool> onSwitch,
                                 int good = 0x2cff14,
                                 int bad = 0x000000)
        {
            return input.TagColor(onSwitch() ? good : bad);
        }

        public string TagStyle(string style)
        {
            return $"<{style}>{input}</{style}>";
        }

        public string RemoveTagColor()
        {
            return Regex.Replace(input.ToString(), "<color(=[^>]*)?>|</color>", "");
        }
    }

    extension(StringBuilder sb)
    {
        public StringBuilder AppendColor(string input, int hex)
        {
            return sb.Append(input.TagColor(hex));
        }

        public StringBuilder AppendLineColor(string input, int hex)
        {
            return sb.AppendLine(input.TagColor(hex));
        }

        public StringBuilder AppendColorEx(string input,
                                           Func<bool> onSwitch,
                                           int good = 0x2cff14,
                                           int bad = 0x000000)
        {
            return sb.Append(input.TagColorEx(onSwitch, good, bad));
        }

        public StringBuilder AppendLineColorEx(string input,
                                               Func<bool> onSwitch,
                                               int good = 0x2cff14,
                                               int bad = 0x000000)
        {
            return sb.AppendLine(input.TagColorEx(onSwitch, good, bad));
        }
    }
}