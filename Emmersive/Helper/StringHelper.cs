using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Emmersive.Helper;

public static class StringHelper
{
    private const int MaxBubbleLines = 4;

    private static readonly string[] _memSizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB"];
    private static readonly char[] _ellipsisTrim = [' ', ',', '.', ';', ':', '!', '?', '，', '。', '、', '；', '：', '！', '？', '…'];

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

            var units = SplitWrapUnits(input);
            if (units.Count == 0) {
                return "";
            }

            var full = segments * segmentsCjk;
            var lines = new List<List<WrapUnit>>();
            var line = new List<WrapUnit>();
            var fill = 0;

            for (var i = 0; i < units.Count; ++i) {
                var unit = units[i];
                line.Add(unit);
                fill += unit.Kind switch {
                    WrapKind.Cjk => segments,
                    WrapKind.Word => segmentsCjk,
                    _ => 0,
                };

                var nextIsPunc = i + 1 < units.Count && units[i + 1].Kind == WrapKind.Punc;
                if (fill >= full && !nextIsPunc) {
                    lines.Add(line);
                    line = [];
                    fill = 0;
                }
            }

            if (line.Count > 0) {
                lines.Add(line);
            }

            if (lines.Count > 1 && lines[^1].Count(u => u.Kind != WrapKind.Punc) <= 1) {
                lines[^2].AddRange(lines[^1]);
                lines.RemoveAt(lines.Count - 1);
            }

            var truncated = lines.Count > MaxBubbleLines;
            if (truncated) {
                lines.RemoveRange(MaxBubbleLines, lines.Count - MaxBubbleLines);
            }

            var sb = new StringBuilder();
            for (var l = 0; l < lines.Count; ++l) {
                if (l > 0) {
                    sb.Append('\n');
                }

                for (var u = 0; u < lines[l].Count; ++u) {
                    var unit = lines[l][u];
                    if (u > 0 && unit.SpaceBefore) {
                        sb.Append(' ');
                    }

                    sb.Append(unit.Text);
                }
            }

            return truncated
                ? sb.ToString().TrimEnd(_ellipsisTrim) + "…"
                : sb.ToString();
        }
    }

    private static List<WrapUnit> SplitWrapUnits(string text)
    {
        var units = new List<WrapUnit>();
        var word = new StringBuilder();
        var wordSpace = false;
        var space = false;

        foreach (var c in text) {
            if (char.IsWhiteSpace(c)) {
                FlushWord();
                space = true;
                continue;
            }

            if (GetCjkKind(c) is { } kind) {
                FlushWord();
                units.Add(new(c.ToString(), kind, space));
                space = false;
                continue;
            }

            if (word.Length == 0) {
                wordSpace = space;
                space = false;
            }

            word.Append(c);
        }

        FlushWord();

        return units;

        void FlushWord()
        {
            if (word.Length == 0) {
                return;
            }

            var token = word.ToString();
            var kind = token.Any(char.IsLetterOrDigit) ? WrapKind.Word : WrapKind.Punc;
            units.Add(new(token, kind, wordSpace));
            word.Clear();
        }
    }

    private static WrapKind? GetCjkKind(char c)
    {
        if (c is >= '\u3040' and <= '\u30FF' or
            >= '\u3400' and <= '\u4DBF' or
            >= '\u4E00' and <= '\u9FFF' or
            >= '\uF900' and <= '\uFAFF') {
            return WrapKind.Cjk;
        }

        if (c is >= '\u3000' and <= '\u303F') {
            return WrapKind.Punc;
        }

        if (c is >= '\uFF00' and <= '\uFFEF') {
            return char.IsLetterOrDigit(c) ? WrapKind.Cjk : WrapKind.Punc;
        }

        return null;
    }

    private enum WrapKind
    {
        Cjk,
        Word,
        Punc,
    }

    private readonly record struct WrapUnit(string Text, WrapKind Kind, bool SpaceBefore);

    public static class Cjk
    {
        private const string CjkCharRange = @"\u4E00-\u9FFF\u3040-\u309F\u30A0-\u30FF\uAC00-\uD7AF";

        public static readonly Regex Char = new($"[{CjkCharRange}]", RegexOptions.Compiled);
    }
}