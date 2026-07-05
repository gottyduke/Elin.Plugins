using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public sealed class CharaMemoryStore
{
    [JsonProperty("u")]
    public int Uid { get; init; }

    [JsonProperty("id")]
    public string UnifiedId { get; set; } = "";

    [JsonProperty("n")]
    public string Name { get; set; } = "";

    [JsonProperty("stm")]
    public List<MemoryEntry> ShortTerm { get; set; } = [];

    [JsonProperty("ltm")]
    public List<MemoryFact> LongTerm { get; set; } = [];

    [JsonProperty("ls")]
    public DateTime LastSummarized { get; set; } = DateTime.MinValue;

    public bool ShouldSummarize =>
        ShortTerm.Count >= EmConfig.Memory.MaxStmEntries.Value * EmConfig.Memory.SummarizeThresholdPercentage.Value &&
        (DateTime.UtcNow - LastSummarized).TotalSeconds > EmConfig.Memory.SummarizeThresholdSeconds.Value;

    public bool IsEmpty => ShortTerm.Count == 0 && LongTerm.Count == 0;

    public void AddStm(string speaker, string content, int turn)
    {
        // skip same speaker
        if (ShortTerm.Count > 0) {
            var last = ShortTerm[^1];
            if (last.Speaker == speaker && last.Content == content) {
                return;
            }
        }

        foreach (var entry in ShortTerm) {
            entry.SentCount = 0;
        }

        ShortTerm.Add(new() {
            Speaker = speaker,
            Content = content,
            Turn = turn,
        });

        // fifo
        while (ShortTerm.Count > EmConfig.Memory.MaxStmEntries.Value) {
            ShortTerm.RemoveAt(0);
        }
    }

    public List<MemoryEntry> GetRecentStm(int count = 0)
    {
        if (count <= 0) {
            count = EmConfig.Memory.MaxStmInContext.Value;
        }

        var maxRepeat = EmConfig.Memory.MaxStmRepeatInContext.Value;
        var result = new List<MemoryEntry>(count);
        for (var i = ShortTerm.Count - 1; i >= 0 && result.Count < count; i--) {
            var entry = ShortTerm[i];
            if (entry.SentCount >= maxRepeat) {
                continue;
            }
            result.Add(entry);
        }

        result.Reverse();
        return result;
    }

    public List<MemoryFact> GetTopLtm(int count = 0)
    {
        if (count <= 0) {
            count = EmConfig.Memory.MaxLtmInContext.Value;
        }

        LongTerm.RemoveAll(f => f.Fact.IsEmptyOrNull);

        var facts = new List<MemoryFact>(LongTerm);
        // by importance desc then by recall count
        facts.Sort((a, b) => {
            var imp = b.Importance.CompareTo(a.Importance);
            return imp != 0 ? imp : b.RecallCount.CompareTo(a.RecallCount);
        });

        var result = new List<MemoryFact>(Math.Min(count, facts.Count));
        for (var i = 0; i < count && i < facts.Count; i++) {
            facts[i].MarkRecalled();
            result.Add(facts[i]);
        }

        return result;
    }

    public void EvictLtm()
    {
        var maxEntries = EmConfig.Memory.MaxLtmEntries.Value;
        if (LongTerm.Count <= maxEntries) {
            return;
        }

        var now = DateTime.UtcNow;

        var scored = LongTerm
            .Select(f => (Fact: f, Score: ScoreFact(f, now)))
            .OrderBy(x => x.Score)
            .ToList();

        var toRemove = new HashSet<MemoryFact>();
        var needToRemove = LongTerm.Count - maxEntries;

        foreach (var (fact, _) in scored) {
            if (needToRemove <= 0) {
                break;
            }

            // pin importance >= 4
            if (fact.Importance >= 4) {
                continue;
            }

            toRemove.Add(fact);
            needToRemove--;
        }

        if (toRemove.Count > 0) {
            LongTerm.RemoveAll(toRemove.Contains);
        }
    }

    private static float ScoreFact(MemoryFact f, DateTime now)
    {
        var score = f.Importance * (1f + Math.Min(f.RecallCount, 5));
        var access = f.LastRecalled ?? f.Created;
        var recency = (float)(now - access).TotalHours switch {
            < 1 => 1.5f,
            < 24 => 1.0f,
            < 168 => 0.7f,
            _ => 0.3f,
        };

        return score * recency;
    }
}