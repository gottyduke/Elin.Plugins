using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emmersive.API.Plugins;
using Emmersive.API.ThirdParty;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emmersive.Contexts.Memory;

internal readonly record struct FactOp(int? Id, string Fact, int? Importance);

internal readonly record struct FactChange(MemoryFactChange Kind, MemoryFact Fact, string? OldFact, int OldImportance);

internal static class MemoryConsolidation
{
    internal const int MaxAddsPerSummary = 3;
    internal const int MaxRewritesPerSummary = 3;

    private const int MaxWrapperDepth = 4;

    private static readonly Regex _thinking =
        new(@"<think(ing)?>[\s\S]*?</think(ing)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static List<FactOp>? Parse(string reply)
    {
        if (FindFacts(reply) is not { } array) {
            return null;
        }

        var ops = new List<FactOp>();
        foreach (var item in array.OfType<JObject>()) {
            var fact = TextOf(item, "fact")?.Trim();
            if (string.IsNullOrEmpty(fact)) {
                continue;
            }

            int? importance = SceneDirector.TryReadNumber(ValueOf(item, "importance"), out var value)
                ? Math.Clamp((int)Math.Round(value), 1, 5)
                : null;

            var rewrite = ValueOf(item, "rewrite");
            if (rewrite is { Type: JTokenType.String }) {
                rewrite = rewrite.ToString().TrimStart('#');
            }

            int? id = SceneDirector.TryReadNumber(rewrite, out var number) && number == Math.Round(number)
                ? (int)number
                : null;

            ops.Add(new(id, fact!, importance));
        }

        return ops;
    }

    internal static List<FactChange> Apply(CharaMemoryStore store,
                                           IReadOnlyList<MemoryFact> known,
                                           IReadOnlyList<FactOp> ops,
                                           int gameTime,
                                           IReadOnlyList<string>? knownTexts = null)
    {
        var changes = new List<FactChange>();
        var rewritten = new HashSet<MemoryFact>();
        var added = 0;
        var updated = 0;

        foreach (var op in ops) {
            if (string.IsNullOrWhiteSpace(op.Fact)) {
                continue;
            }

            if (op.Id is { } id and >= 1 && id <= known.Count) {
                var target = known[id - 1];
                if (!store.LongTerm.Contains(target) ||
                    (knownTexts is not null && target.Fact != knownTexts[id - 1]) ||
                    !rewritten.Add(target)) {
                    continue;
                }

                target.MarkRecalled();

                if (MemoryRelevance.Normalize(target.Fact) == MemoryRelevance.Normalize(op.Fact) ||
                    updated >= MaxRewritesPerSummary) {
                    continue;
                }

                updated++;
                var oldFact = target.Fact;
                var oldImportance = target.Importance;
                target.Fact = op.Fact;
                target.Importance = op.Importance ?? target.Importance;
                target.GameTime = gameTime;
                changes.Add(new(MemoryFactChange.Updated, target, oldFact, oldImportance));
                continue;
            }

            if (store.LongTerm.FirstOrDefault(f =>
                MemoryRelevance.IsNearDuplicate(f.Fact, op.Fact, MemoryRelevance.FactCjkThreshold)) is { } existing) {
                existing.MarkRecalled();
                continue;
            }

            if (added >= MaxAddsPerSummary) {
                continue;
            }

            var fact = new MemoryFact {
                Fact = op.Fact,
                Importance = op.Importance ?? 1,
                GameTime = gameTime,
            };
            store.LongTerm.Add(fact);
            added++;
            changes.Add(new(MemoryFactChange.Added, fact, null, 0));
        }

        return changes;
    }

    private static JArray? FindFacts(string reply)
    {
        var text = SceneDirector.StripMarkdownFence(_thinking.Replace(reply, "")).Trim();
        JArray? empty = null;

        for (var start = 0; start >= 0 && start < text.Length; start = text.IndexOfAny(['[', '{'], start + 1)) {
            switch (ReadFirst(text[start..])) {
                case JObject single when TextOf(single, "fact") is not null:
                    return new(single);
                case { } token when FindFactArray(token, 0) is { } facts:
                    return facts;
                case { } token when HasEmptyArray(token, 0):
                    empty ??= [];
                    break;
            }
        }

        return empty;

        static JToken? ReadFirst(string json)
        {
            try {
                using var reader = new JsonTextReader(new StringReader(json));
                return JToken.ReadFrom(reader);
            } catch {
                return null;
                // noexcept
            }
        }
    }

    private static JToken? ValueOf(JObject obj, string name)
    {
        return obj.GetValue(name, StringComparison.OrdinalIgnoreCase);
    }

    private static string? TextOf(JObject obj, string name)
    {
        return ValueOf(obj, name) is { Type: JTokenType.String } token ? token.Value<string>() : null;
    }

    private static JArray? FindFactArray(JToken token, int depth)
    {
        if (depth > MaxWrapperDepth) {
            return null;
        }

        return token switch {
            JArray array when array.Any(i => i is JObject obj && TextOf(obj, "fact") is not null) => array,
            JArray array => array.Select(i => FindFactArray(i, depth + 1)).FirstOrDefault(hit => hit is not null),
            JObject obj => obj.Properties().Select(p => FindFactArray(p.Value, depth + 1)).FirstOrDefault(hit => hit is not null),
            _ => null,
        };
    }

    private static bool HasEmptyArray(JToken token, int depth)
    {
        if (depth > MaxWrapperDepth) {
            return false;
        }

        return token switch {
            JArray array => array.Count == 0,
            JObject obj => obj.Properties().Any(p => HasEmptyArray(p.Value, depth + 1)),
            _ => false,
        };
    }
}