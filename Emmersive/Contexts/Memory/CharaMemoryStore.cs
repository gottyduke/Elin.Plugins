using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public sealed class CharaMemoryStore
{
    private const int PinnedLtmCount = 5;
    private const int StmDedupLookback = 4;
    private const int OwnLinesAlwaysSent = 2;
    private const int OwnLinesFreshMinutes = 6 * 60;
    private const int RecentRepeatMinutes = 3 * 60;
    private const int MaxFillerFacts = 2;
    private const int CoreImportance = 5;
    private const float RelevanceWeight = 1.5f;
    private const float RecencyWeight = 0.5f;
    private const float RecencyDecayPerDay = 0.95f;
    private const float LegacyRecency = 0.3f;

    private static readonly object _entryTimeLock = new();
    private static long _lastEntryTicks;

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

    internal static DateTime NextEntryTime(DateTime now)
    {
        lock (_entryTimeLock) {
            _lastEntryTicks = Math.Max(now.Ticks, _lastEntryTicks + 1);
            return new(_lastEntryTicks, DateTimeKind.Utc);
        }
    }

    public void AddStm(string speaker, string content, int turn)
    {
        AddStm(speaker, content, turn, EClass.world.date.GetRaw(), EmConfig.Memory.MaxStmEntries.Value);
    }

    internal void AddStm(string speaker, string content, int turn, int gameTime, int maxEntries)
    {
        var isPlayer = speaker == MemoryManager.PlayerSpeaker;
        if (isPlayer) {
            foreach (var entry in ShortTerm) {
                entry.SentCount = 0;
            }
        }

        if (!isPlayer) {
            var normalized = MemoryRelevance.Normalize(content);
            for (var i = ShortTerm.Count - 1; i >= 0 && i >= ShortTerm.Count - StmDedupLookback; i--) {
                var entry = ShortTerm[i];
                if (entry.Speaker == speaker && MemoryRelevance.Normalize(entry.Content) == normalized) {
                    return;
                }
            }
        }

        ShortTerm.Add(new() {
            Speaker = speaker,
            Content = content,
            Turn = turn,
            GameTime = gameTime,
            Time = NextEntryTime(DateTime.UtcNow),
        });

        // fifo
        while (ShortTerm.Count > maxEntries) {
            ShortTerm.RemoveAt(0);
        }
    }

    public List<MemoryEntry> GetRecentStm(int count = 0)
    {
        if (count <= 0) {
            count = EmConfig.Memory.MaxStmInContext.Value;
        }

        return GetRecentStm(count, EmConfig.Memory.MaxStmRepeatInContext.Value, Name, EClass.world.date.GetRaw());
    }

    internal List<MemoryEntry> GetRecentStm(int count, int maxRepeat, string? ownerSpeaker, int gameTime)
    {
        var result = new List<MemoryEntry>(count);
        var ownSeen = 0;

        for (var i = ShortTerm.Count - 1; i >= 0 && result.Count < count; i--) {
            var entry = ShortTerm[i];
            var isOwn = entry.Speaker == ownerSpeaker;
            var ownRecent = isOwn &&
                            ownSeen < OwnLinesAlwaysSent &&
                            entry.GameTime > 0 &&
                            gameTime - entry.GameTime <= OwnLinesFreshMinutes;

            if (entry.SentCount < maxRepeat || ownRecent) {
                result.Add(entry);
            }

            if (isOwn) {
                ownSeen++;
            }
        }

        result.Reverse();
        return result;
    }

    public List<MemoryFact> GetTopLtm(int count = 0)
    {
        if (count <= 0) {
            count = EmConfig.Memory.MaxLtmInContext.Value;
        }

        return [
            ..LongTerm
                .Where(f => !f.Fact.IsWhiteSpaceOrNull)
                .OrderByDescending(f => f.Importance)
                .ThenBy(f => f.LastRecalled ?? DateTime.MinValue)
                .Take(count),
        ];
    }

    internal List<MemoryFact> RecallLtm(RecallRequest request)
    {
        if (request.Max <= 0) {
            return [];
        }

        var relevant = new List<(MemoryFact Fact, float Score)>();
        var others = new List<(MemoryFact Fact, float Score)>();

        foreach (var fact in LongTerm) {
            if (fact.Fact.IsWhiteSpaceOrNull) {
                continue;
            }

            var relevance = MemoryRelevance.Relevance(fact.Tokens, request.Lines, request.Ignore, out var triggered);
            if (request.Tick <= fact.SaidUntilTick) {
                MemoryRelevance.Relevance(fact.Tokens, request.PlayerLines, request.Ignore, out var asked);
                if (!asked) {
                    continue;
                }
            }

            var score = RelevanceWeight * relevance + (fact.Importance - 1) / 4f +
                        RecencyWeight * Recency(fact.GameTime, request.GameTime);
            (triggered ? relevant : others).Add((fact, score));
        }

        var recalled = relevant
            .OrderByDescending(f => f.Score)
            .Take(request.Max)
            .Select(f => f.Fact)
            .ToList();

        foreach (var fact in recalled) {
            fact.MarkRecalled();
        }

        var budget = Math.Min(MaxFillerFacts, request.Max - recalled.Count);
        var ranked = others.OrderByDescending(f => f.Score).Select(f => f.Fact).ToList();

        var core = ranked
            .Where(f => f.Importance >= CoreImportance && !(request.Tick - f.LastInjectedTick <= 1))
            .OrderBy(f => f.LastInjectedTick ?? int.MinValue)
            .FirstOrDefault();
        if (budget > 0 && core is not null) {
            recalled.Add(core);
            budget--;
        }

        recalled.AddRange(ranked
            .Where(f => f != core && !(request.Tick - f.LastInjectedTick <= request.Cooldown))
            .Take(Math.Max(0, budget)));

        foreach (var fact in recalled) {
            fact.LastInjectedTick = request.Tick;
        }

        return recalled;
    }

    internal void MarkSaid(string line, IEnumerable<string> ignoreNames, int tick, int cooldown)
    {
        var lineTokens = MemoryRelevance.Tokenize(line);
        if (lineTokens.Count == 0) {
            return;
        }

        var ignore = MemoryRelevance.IgnoreTokens(ignoreNames);
        foreach (var fact in LongTerm) {
            if (fact.Fact.IsWhiteSpaceOrNull) {
                continue;
            }

            if (MemoryRelevance.Covers(fact.Tokens, lineTokens, ignore)) {
                fact.SaidUntilTick = tick + cooldown;
            }
        }
    }

    internal bool HasRecentRepeat(string content, int lookback, int gameTime)
    {
        if (!MemoryRelevance.IsSubstantive(content)) {
            return false;
        }

        for (var i = ShortTerm.Count - 1; i >= 0 && i >= ShortTerm.Count - lookback; i--) {
            var entry = ShortTerm[i];
            if (entry.GameTime > 0 &&
                gameTime - entry.GameTime <= RecentRepeatMinutes &&
                MemoryRelevance.IsNearDuplicate(entry.Content, content)) {
                return true;
            }
        }

        return false;
    }

    public void EvictLtm()
    {
        EvictLtm(EmConfig.Memory.MaxLtmEntries.Value, DateTime.UtcNow);
    }

    internal List<MemoryFact> EvictLtm(int maxEntries, DateTime now)
    {
        if (LongTerm.Count <= maxEntries) {
            return [];
        }

        var pinned = LongTerm
            .Where(f => f.Importance >= 4)
            .OrderBy(f => f.Created)
            .Take(PinnedLtmCount)
            .ToHashSet();

        var toRemove = LongTerm
            .Where(f => !pinned.Contains(f))
            .OrderBy(f => f.Importance)
            .ThenBy(f => ScoreFact(f, now))
            .Take(LongTerm.Count - maxEntries)
            .ToList();

        if (toRemove.Count > 0) {
            var removing = toRemove.ToHashSet();
            LongTerm.RemoveAll(removing.Contains);
        }

        return toRemove;
    }

    private static float Recency(int factTime, int now)
    {
        if (factTime <= 0) {
            return LegacyRecency;
        }

        var days = Math.Max(0, now - factTime) / 1440f;
        return (float)Math.Pow(RecencyDecayPerDay, days);
    }

    private static float ScoreFact(MemoryFact f, DateTime now)
    {
        var score = f.Importance * (1f + 0.25f * Math.Min(f.RecallCount, 4));
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

internal sealed record RecallRequest(
    IReadOnlyList<HashSet<string>> Lines,
    HashSet<string> Ignore,
    int Tick,
    int Max,
    int Cooldown,
    int GameTime,
    IReadOnlyList<HashSet<string>> PlayerLines);