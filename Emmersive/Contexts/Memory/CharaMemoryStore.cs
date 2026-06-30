using System;
using System.Collections.Generic;
using Emmersive.Helper;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public sealed class CharaMemoryStore
{
    [JsonProperty("u")]
    public int Uid { get; init; }

    [JsonProperty("id")]
    public string UnifiedId { get; set; } = string.Empty;

    [JsonProperty("n")]
    public string Name { get; set; } = string.Empty;

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
            count = EmConfig.Memory.MaxStmEntries.Value;
        }

        var result = new List<MemoryEntry>(count);
        for (var i = ShortTerm.Count - 1; i >= 0 && result.Count < count; i--) {
            result.Add(ShortTerm[i]);
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
}