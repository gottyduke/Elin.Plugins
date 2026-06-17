using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace EModding.Helper;

public static class StringHelper
{
    public static string MergeOverlap(string lhs, string rhs, string delimiter = " ")
    {
        var maxOverlap = 0;
        for (var i = 1; i <= Math.Min(lhs.Length, rhs.Length); ++i) {
            if (lhs.EndsWith(rhs[..i], StringComparison.OrdinalIgnoreCase)) {
                maxOverlap = i;
            }
        }

        return $"{lhs}{delimiter}{rhs[maxOverlap..]}";
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
            return string.IsNullOrEmpty(input) || input.RemoveTagColor().Length <= length ? input : $"{input[..length]} ...";
        }

        public string TruncateAllLines(int length)
        {
            return input.SplitByNewline().Select(s => s.Truncate(length)).Join(s => s, "\n");
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
            var hexString = color <= 0xffffff
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
}