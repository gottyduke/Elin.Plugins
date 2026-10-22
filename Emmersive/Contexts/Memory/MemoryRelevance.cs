using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emmersive.Contexts.Memory;

internal static class MemoryRelevance
{
    internal const float TriggerThreshold = 0.3f;
    internal const float CoverThreshold = 0.5f;

    private const int MinSubstantiveWords = 3;
    private const int MinSubstantiveCjk = 5;
    internal const float LineCjkThreshold = 0.8f;
    internal const float FactCjkThreshold = 0.85f;

    private const int MinFuzzyWords = 4;
    private const int MinFuzzyCjk = 6;
    private const float FuzzyWordThreshold = 0.9f;

    internal static HashSet<string> IgnoreTokens(IEnumerable<string> names)
    {
        var ignore = new HashSet<string>(MemoryLexicon.Current.PlayerTokens, StringComparer.Ordinal);
        foreach (var name in names) {
            ignore.UnionWith(Tokenize(name));
        }

        return ignore;
    }

    internal static float Relevance(HashSet<string> fact,
                                    IReadOnlyList<HashSet<string>> lines,
                                    HashSet<string> ignore,
                                    out bool triggered)
    {
        var best = 0f;
        var factWeight = Weigh(fact, ignore);

        if (factWeight > 0) {
            foreach (var line in lines) {
                var shared = 0;
                var strong = false;
                foreach (var token in line) {
                    if (ignore.Contains(token) || !fact.Contains(token)) {
                        continue;
                    }

                    shared += Weight(token);
                    strong |= Weight(token) > 1;
                }

                if (strong) {
                    best = Math.Max(best, (float)shared / Math.Min(factWeight, Weigh(line, ignore)));
                }
            }
        }

        triggered = best >= TriggerThreshold;
        return best;
    }

    internal static bool Covers(HashSet<string> fact, HashSet<string> line, HashSet<string> ignore)
    {
        var factStrong = fact.Count(t => !ignore.Contains(t) && Weight(t) > 1);

        var shared = 0;
        var sharedStrong = 0;
        foreach (var token in line) {
            if (ignore.Contains(token) || !fact.Contains(token)) {
                continue;
            }

            shared += Weight(token);
            if (Weight(token) > 1) {
                sharedStrong++;
            }
        }

        return sharedStrong > 0 &&
               sharedStrong >= Math.Min(2, factStrong) &&
               shared >= Weigh(fact, ignore) * CoverThreshold;
    }

    internal static HashSet<string> Tokenize(string? text)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text)) {
            return tokens;
        }

        var lexicon = MemoryLexicon.Current;
        var word = new StringBuilder();
        var cjk = new StringBuilder();

        foreach (var c in text!) {
            if (IsCjk(c)) {
                FlushWord();
                cjk.Append(c);
            } else if (char.IsLetterOrDigit(c)) {
                FlushCjk();
                word.Append(char.ToLowerInvariant(c));
            } else if (c is '\'' or '’' && word.Length > 0) {
                word.Append('\'');
            } else {
                FlushWord();
                FlushCjk();
            }
        }

        FlushWord();
        FlushCjk();

        return tokens;

        void FlushWord()
        {
            var w = word.ToString();
            word.Clear();

            var apostrophe = w.IndexOf('\'');
            if (apostrophe >= 0) {
                w = w[..apostrophe];
            }

            if (w.Length < 3 || lexicon.StopWords.Contains(w)) {
                return;
            }

            var stem = Stem(w);
            if (stem.Length >= 3) {
                tokens.Add(stem);
            }
        }

        void FlushCjk()
        {
            for (var i = 0; i < cjk.Length; i++) {
                var c = cjk[i];
                if (IsHan(c) && !IsStopHan(c)) {
                    tokens.Add(c.ToString());
                }

                if (i + 1 >= cjk.Length) {
                    continue;
                }

                var next = cjk[i + 1];
                if (IsStopHan(c) || IsStopHan(next) || IsHiragana(c) || IsHiragana(next)) {
                    continue;
                }

                var bigram = cjk.ToString(i, 2);
                if (!lexicon.StopBigrams.Contains(bigram)) {
                    tokens.Add(bigram);
                }
            }

            cjk.Clear();
        }

        bool IsStopHan(char c) => lexicon.StopHan.IndexOf(c) >= 0;
    }

    internal static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text) {
            if (char.IsLetterOrDigit(c)) {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }

    internal static bool IsSubstantive(string text)
    {
        var cjk = text.Count(IsCjk);
        return cjk > 0 ? cjk >= MinSubstantiveCjk : Words(text).Count >= MinSubstantiveWords;
    }

    internal static bool IsNearDuplicate(string a, string b, float cjkThreshold = LineCjkThreshold)
    {
        var na = Normalize(a);
        var nb = Normalize(b);

        if (na.Length == 0 || nb.Length == 0) {
            return false;
        }

        if (na == nb) {
            return true;
        }

        var significant = MemoryLexicon.Current.SignificantChars;

        if (a.Any(IsCjk) || b.Any(IsCjk)) {
            return Math.Min(na.Length, nb.Length) >= MinFuzzyCjk &&
                   !significant.Any(c => (a.IndexOf(c) >= 0) != (b.IndexOf(c) >= 0)) &&
                   Jaccard(Bigrams(na), Bigrams(nb)) >= cjkThreshold;
        }

        var wordsA = Words(a);
        var wordsB = Words(b);
        var differing = wordsA.Where(w => !wordsB.Contains(w)).Concat(wordsB.Where(w => !wordsA.Contains(w)));

        return Math.Min(wordsA.Count, wordsB.Count) >= MinFuzzyWords &&
               !differing.Any(w => MemoryLexicon.Current.NegationWords.Contains(w) || w.Any(c => significant.IndexOf(c) >= 0)) &&
               Jaccard(wordsA, wordsB) >= FuzzyWordThreshold;
    }

    private static string Stem(string w)
    {
        if (w.Length > 4 && w.EndsWith("ies", StringComparison.Ordinal)) {
            w = w[..^2];
        } else if (w.Length > 5 && w.EndsWith("ing", StringComparison.Ordinal)) {
            w = w[..^3];
        } else if (w.Length > 4 && w.EndsWith("ed", StringComparison.Ordinal)) {
            w = w[..^2];
        } else if (w.Length > 4 && w.EndsWith("es", StringComparison.Ordinal)) {
            w = w[..^2];
        } else if (w.Length > 3 && w.EndsWith("s", StringComparison.Ordinal) && !w.EndsWith("ss", StringComparison.Ordinal)) {
            w = w[..^1];
        }

        if (w.Length > 3 && w.EndsWith("e", StringComparison.Ordinal)) {
            w = w[..^1];
        } else if (w.Length > 3 && w.EndsWith("y", StringComparison.Ordinal)) {
            w = w[..^1] + "i";
        }

        return w;
    }

    private static HashSet<string> Words(string text)
    {
        var words = new HashSet<string>(StringComparer.Ordinal);
        var sb = new StringBuilder();

        foreach (var c in text) {
            if (char.IsLetterOrDigit(c)) {
                sb.Append(char.ToLowerInvariant(c));
            } else if (sb.Length > 0) {
                words.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0) {
            words.Add(sb.ToString());
        }

        return words;
    }

    private static HashSet<string> Bigrams(string text)
    {
        var bigrams = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < text.Length - 1; i++) {
            bigrams.Add(text.Substring(i, 2));
        }

        return bigrams;
    }

    private static float Jaccard(HashSet<string> a, HashSet<string> b)
    {
        var shared = a.Count(b.Contains);
        var union = a.Count + b.Count - shared;
        return union > 0 ? (float)shared / union : 0f;
    }

    private static int Weight(string token)
    {
        return token.Length == 1 ? 1 : 2;
    }

    private static int Weigh(HashSet<string> tokens, HashSet<string> ignore)
    {
        return tokens.Where(t => !ignore.Contains(t)).Sum(Weight);
    }

    private static bool IsHan(char c)
    {
        return c is >= '一' and <= '鿿' or >= '㐀' and <= '䶿';
    }

    private static bool IsHiragana(char c)
    {
        return c is >= '぀' and <= 'ゟ';
    }

    private static bool IsCjk(char c)
    {
        return IsHan(c) || c is >= '぀' and <= 'ヿ' or >= '가' and <= '힯';
    }
}