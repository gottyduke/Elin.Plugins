using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Emmersive.Helper;

public static class StringHelper
{
    private static readonly string[] _memSizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string ToAllocateString(this long bytes)
    {
        switch (bytes) {
            case < 0:
                return "-" + (-bytes).ToAllocateString();
            case 0:
                return "0 B";
        }

        var mag = (int)Math.Log(bytes, 1024);
        var size = bytes / Math.Pow(1024, mag);

        return $"{size:0.##} {_memSizeSuffixes[mag]}";
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

        public string Wrap(int segments = 7, int segmentsCjk = 14)
        {
            input = input.TrimNewLines().Replace("\n", "").Trim();
            var matches = Cjk.Splitter.Matches(input);

            if (matches.Count == 0) {
                return "";
            }

            var sb = new StringBuilder();

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
    }
}