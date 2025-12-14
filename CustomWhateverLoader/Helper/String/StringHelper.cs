using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class StringHelper
{
    private static readonly string[] _memSizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string ToAllocateString(this long bytes)
    {
        switch (bytes) {
            case < 0:
                return "-" + ToAllocateString(-bytes);
            case 0:
                return "0 B";
        }

        var mag = (int)Math.Log(bytes, 1024);
        var size = bytes / Math.Pow(1024, mag);

        return $"{size:0.##} {_memSizeSuffixes[mag]}";
    }

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

    public static class Cjk
    {
        private const string CjkCharRange = @"\u4E00-\u9FFF\u3040-\u309F\u30A0-\u30FF\uAC00-\uD7AF";

        public static readonly Regex Splitter = new(
            $"[{CjkCharRange}]" +
            @"|[A-Za-z0-9'\-]+" +
            @"|[，。！？、；：「」『』（）《》〈〉【】〔〕—…～·]+" +
            @"|[^\s]",
            RegexOptions.Compiled);

        public static readonly Regex Char = new($"[{CjkCharRange}]", RegexOptions.Compiled);

        public static readonly Regex Punc = new(@"[，。！？、；：「」『』（）《》〈〉【】〔〕—…～·]", RegexOptions.Compiled);
    }

    extension(string input)
    {
        public bool IsEmptyOrNull => string.IsNullOrEmpty(input);
        public bool IsWhiteSpaceOrNull => string.IsNullOrWhiteSpace(input);

        public string OrIfEmpty(string fallback)
        {
            return string.IsNullOrEmpty(input) ? fallback : input;
        }

        public string OrIfWhiteSpace(string fallback)
        {
            return string.IsNullOrWhiteSpace(input) ? fallback : input;
        }

        public string Capitalize()
        {
            Span<char> buf = input.ToCharArray();
            buf[0] = char.ToUpperInvariant(buf[0]);
            return buf.ToString();
        }

        public string Truncate(int length)
        {
            return input.IsEmptyOrNull || input.RemoveTagColor().Length <= length ? input : $"{input[..length]} ...";
        }

        public string TruncateAllLines(int length)
        {
            return input.SplitLines().Select(s => s.Truncate(length)).Join(s => s, "\n");
        }

        public string[] SplitLines()
        {
            return input.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public string Wrap(int segments = 7, int segmentsCjk = 14)
        {
            input = input.TrimNewLines().Replace("\n", "").Trim();
            var matches = Cjk.Splitter.Matches(input);

            if (matches.Count == 0) {
                return "";
            }

            using var sb = StringBuilderPool.Get();

            var count = 0;
            var lastCjk = false;
            var inCjkSegment = false;

            for (var i = 0; i < matches.Count; ++i) {
                var token = matches[i].Value;
                var isCjk = Cjk.Char.IsMatch(token);
                var isPunc = Cjk.Punc.IsMatch(token);

                if (count > 0 && !isCjk && !lastCjk) {
                    sb.Append(' ');
                }

                sb.Append(token);

                if (!isPunc) {
                    count++;
                }

                if (!token.IsEmptyOrNull) {
                    inCjkSegment = isCjk;
                }

                var limit = inCjkSegment ? segmentsCjk : segments;

                var nextIsPunc = false;
                if (i + 1 < matches.Count) {
                    nextIsPunc = Cjk.Punc.IsMatch(matches[i + 1].Value);
                }

                if (count >= limit && !nextIsPunc) {
                    sb.AppendLine();
                    count = 0;
                }

                lastCjk = isCjk || isPunc;
            }

            var result = sb.ToString().TrimEnd();

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) {
                return result;
            }

            var lastLine = lines[^1].Trim();
            var lastSegments = Cjk.Char.Replace(lastLine, " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Length;

            if (lastSegments > 1) {
                return result;
            }

            // merge back
            var firstChar = lastLine.Length > 0 ? lastLine[0] : '\0';
            var startsWithPunc = firstChar is '!' or '?' or '.' or ',' or '…' or ':' or ';';

            lines[^2] = lines[^2].TrimEnd() + (startsWithPunc ? "" : " ") + lastLine;
            result = string.Join('\n', lines[..^1]);

            return result;
        }

        public object AsTypeOf(Type type)
        {
            if (type == typeof(string)) {
                return input;
            }

            if (type == typeof(bool)) {
                return input == "1" || input.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return Convert.ChangeType(input, type, CultureInfo.InvariantCulture);
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