using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TextureExpand.Rules;

internal readonly record struct RuleLayer(int Index, int Priority, string Path, Vector2Int Offset);

internal sealed class ReplaceRule
{
    private static readonly Regex _priorityPattern = new(@"#p-(-?\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _layerPattern = new(@"#layer-(-?\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _offsetPattern =
        new(@"#offset-(-?\d+)-(-?\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    internal readonly List<RuleLayer> Layers = [];

    private readonly List<RuleJudge> _judges = [];

    internal string Key = "";

    internal int? LayerIndex;
    internal Vector2Int LayerOffset = Vector2Int.zero;

    internal int Priority;

    internal int ReplacedTile = -1;
    private string? _concatFilePath;
    private string? _concatMatchPhrase;

    internal string ConcatFilePath => _concatFilePath ??= string.Join(",", Layers.Select(l => l.Path));
    internal string ConcatMatchPhrase => _concatMatchPhrase ??= string.Join(",", _judges.Select(j => j.MatchPhrase));
    internal bool HasJudges => _judges.Count > 0;

    internal static ReplaceRule? Create(string charaId, string key, string filePath)
    {
        var rule = new ReplaceRule { Key = key };

        var match = _priorityPattern.Match(key);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var priority)) {
            rule.Priority = priority;
        }

        match = _layerPattern.Match(key);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var layer)) {
            rule.LayerIndex = layer;

            match = _offsetPattern.Match(key);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var x) &&
                int.TryParse(match.Groups[2].Value, out var y)) {
                rule.LayerOffset = new(x, y);
            }
        }

        rule.Layers.Add(new(rule.LayerIndex ?? 0, rule.Priority, filePath, rule.LayerOffset));
        return rule.ParseJudges(charaId, key) ? rule : null;
    }

    internal static ReplaceRule? AddLayer(ReplaceRule rule, List<ReplaceRule> layerRules)
    {
        if (layerRules.Any(x => x.LayerIndex is null || x.Layers.Count != 1)) {
            TextureExpand.LogError("not layer rule: " + string.Join(",", layerRules.Select(x => x.Key)));
            return null;
        }

        var first = layerRules[0];
        var inPlace = rule.ContainsJudgeAll(first);
        var merged = rule;

        if (!inPlace) {
            merged = rule.Clone();

            var shared = 0;
            foreach (var judge in first._judges) {
                if (merged._judges.Any(j => j.MatchPhrase == judge.MatchPhrase)) {
                    shared++;
                    continue;
                }

                if (!judge.Keyword.AllowMulti && merged._judges.Any(j => j.Keyword == judge.Keyword)) {
                    return null;
                }

                if (merged._judges.Any(j => j.IsConflict(judge.MatchPhrase))) {
                    return null;
                }

                merged._judges.Add(judge);
                merged._concatMatchPhrase = null;
                shared++;
            }

            if (shared <= 0) {
                return null;
            }
        }

        layerRules.Sort((a, b) => (a.LayerIndex ?? 0).CompareTo(b.LayerIndex ?? 0));

        merged.Key += "," + string.Join(",", layerRules.Select(x => x.Key));
        merged.Priority += layerRules.Sum(x => x.Priority);

        foreach (var layerRule in layerRules) {
            var index = (int)layerRule.LayerIndex!;

            var existing = merged.Layers.FirstOrDefault(l => l.Index == index);
            if (existing.Priority >= layerRule.Priority) {
                continue;
            }

            merged.Layers.Remove(existing);

            var layer = new RuleLayer(index, layerRule.Priority, layerRule.ConcatFilePath, layerRule.LayerOffset);
            if (index < 0) {
                merged.Layers.Insert(merged.Layers.Count - 1, layer);
            } else {
                merged.Layers.Add(layer);
            }

            merged._concatFilePath = null;
        }

        return inPlace ? null : merged;
    }

    internal bool IsValid(Chara chara)
    {
        foreach (var judge in _judges) {
            if (!judge.IsMatch(chara)) {
                return false;
            }
        }

        return true;
    }

    internal bool ContainsJudgeAll(ReplaceRule other)
    {
        return other._judges.All(o => _judges.Any(j => j.MatchPhrase == o.MatchPhrase));
    }

    private ReplaceRule Clone()
    {
        var clone = new ReplaceRule {
            Priority = Priority,
            ReplacedTile = ReplacedTile,
            LayerIndex = LayerIndex,
            LayerOffset = LayerOffset,
            Key = Key,
        };
        clone.Layers.AddRange(Layers);
        clone._judges.AddRange(_judges);
        return clone;
    }

    private bool ParseJudges(string charaId, string key)
    {
        var used = Trigger.None;
        foreach (var keyword in RuleKeywords.All) {
            switch (TryAddJudges(keyword, key)) {
                case null:
                    return false;
                case true:
                    used |= keyword.Trigger;
                    break;
            }
        }

        RuleKeywords.AddTarget(charaId, used);
        return true;
    }

    private bool? TryAddJudges(RuleKeyword keyword, string key)
    {
        var added = 0;
        var startAt = 0;
        while (true) {
            var match = keyword.Pattern.Match(key, startAt);
            if (!match.Success) {
                return added > 0;
            }

            var phrase = match.Value;
            var closing = match.Groups[4].Value;
            if (!closing.IsEmpty()) {
                phrase = phrase[..^closing.Length];
            }

            startAt = match.Index + match.Length - 1;

            if (_judges.Any(j => j.MatchPhrase == phrase)) {
                continue;
            }

            if (!keyword.AllowMulti && _judges.Any(j => j.Keyword == keyword)) {
                continue;
            }

            var expected = match.Groups[1].Value.IsEmpty();
            var predicate = keyword.Factory(match.Groups[3].Captures, expected);
            if (predicate is null) {
                return null;
            }

            _judges.Add(new(phrase, keyword, predicate));
            Priority += keyword.Priority;
            _concatMatchPhrase = null;
            added++;
        }
    }
}