using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.ThirdParty;
using Emmersive.Helper;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public sealed class MemoryManager
{
    private int _summarizeInProgress;

    [ElinGameIOProperty("emmersive_memory_store")]
    private static ConcurrentDictionary<int, CharaMemoryStore> Stores
    {
        get => field ??= [];
        set;
    }

    public IReadOnlyList<CharaMemoryStore> AllStores => Stores.Values.ToList();

    public static MemoryManager Instance => field ??= new();

    public CharaMemoryStore GetOrCreate(Chara chara)
    {
        return Stores.GetOrAdd(chara.uid, _ => {
            var store = new CharaMemoryStore {
                Uid = chara.uid,
                UnifiedId = chara.UnifiedId,
                Name = chara.NameSimple,
            };
            return store;
        });
    }

    public CharaMemoryStore? Get(int uid)
    {
        return Stores.GetValueOrDefault(uid);
    }

    public void RecordTalk(Chara chara, string content)
    {
        if (!EmConfig.Memory.Enabled.Value) {
            return;
        }

        if (!chara.Profile.IsImportant && !chara.IsPC) {
            return;
        }

        var store = GetOrCreate(chara);
        var speaker = chara.IsPC ? "Player" : chara.NameSimple;
        store.AddStm(speaker, content, chara.turn);

        if (store.ShouldSummarize && chara.Profile.AllowSummarize) {
            TriggerSummarizeAsync(store, CancellationToken.None).ForgetEx();
        }
    }

    public bool HasRecentTalk(Chara chara, string content, int lookback = 3)
    {
        var store = Get(chara.uid);
        if (store is not { ShortTerm.Count: > 0 }) {
            return false;
        }

        var recent = store.GetRecentStm(lookback);
        return recent.Any(e => e.Content == content);
    }

    public void ClearMemory(Chara chara)
    {
        Stores.TryRemove(chara.uid, out _);
    }

    public async UniTask<bool> TriggerSummarizeAsync(CharaMemoryStore store, CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _summarizeInProgress, 1, 0) != 0) {
            return false;
        }

        try {
            if (!EmAi.IsAvailable) {
                return false;
            }

            var stmCopy = store.ShortTerm.ToList();
            if (stmCopy.Count == 0) {
                return false;
            }

            stmCopy = DeduplicateStm(stmCopy);

            var chara = EClass.game.cards.Find(store.Uid);
            if (chara is null) {
                return false;
            }

            var recentTalks = string.Join("\n", stmCopy.Select(e => e.ToString()));

            var prompt = new SystemContext("Emmersive/MemoryPrompt.txt").Build();
            var background = new BackgroundContext(chara).Build();
            var user = $"NPC: {store.Name}\n" +
                       $"Background: {background}\n" +
                       $"Recent conversations: {recentTalks}\n" +
                       "Extract key facts:";

            var requestTask = EmAi.SendWithReportAsync(prompt.ToString(), user, null, ct)
                .Preserve();

            var timeout = EmConfig.Policy.Timeout.Value;
            var (hasResultLeft, report) = await UniTask.WhenAny(
                requestTask,
                UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: ct));

            if (!hasResultLeft || report is null) {
                EmMod.Warn<MemoryManager>($"summarization timed out after {timeout}s for {store.Name}");
                return false;
            }

            if (!report.Success) {
                EmMod.Warn<MemoryManager>($"summarization failed for {store.Name}: {report.ErrorReason}");
                return false;
            }

            if (report.Content.IsEmptyOrNull) {
                return false;
            }

            var facts = ParseFacts(report.Content);
            if (facts.Count > 0) {
                // dedup stm -> ltm
                var existingFacts = store.LongTerm
                    .Select(f => f.Fact)
                    .ToHashSet(StringComparer.Ordinal);

                var newFacts = facts
                    .Where(f => !existingFacts.Contains(f.Fact))
                    .ToList();

                if (newFacts.Count > 0) {
                    store.LongTerm.AddRange(newFacts);
                }

                store.LongTerm.RemoveAll(f => f.Fact.IsEmptyOrNull);
                store.EvictLtm();
                store.LastSummarized = DateTime.UtcNow;

                var keepCount = Math.Min(EmConfig.Memory.MaxStmEntriesAfterSummarization.Value, store.ShortTerm.Count);
                store.ShortTerm.RemoveRange(0, store.ShortTerm.Count - keepCount);

                EmMod.Log<MemoryManager>($"summarized {newFacts.Count} facts for {store.Name} " +
                                         $"({report.LatencyMs:F0}ms, {report.TokensInput}+{report.TokensOutput} tokens)");
            }
        } catch (Exception ex) {
            EmMod.Warn<MemoryManager>($"summarization failed for {store.Name}: {ex.Message}");
            return false;
        } finally {
            Interlocked.Exchange(ref _summarizeInProgress, 0);
        }

        return true;
    }

    private static List<MemoryFact> ParseFacts(string json)
    {
        var raw = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json.Trim());

        return raw?.Select(item => new MemoryFact {
                Fact = item.TryGetValue("fact", out var f) ? f?.ToString() ?? "" : "",
                Importance = item.TryGetValue("importance", out var i) && int.TryParse(i?.ToString(), out var iv)
                    ? Math.Clamp(iv, 1, 5)
                    : 1,
            })
            .Where(f => !f.Fact.IsEmptyOrNull)
            .ToList() ?? [];
    }

    private static List<MemoryEntry> DeduplicateStm(List<MemoryEntry> entries)
    {
        if (entries.Count <= 1) {
            return entries;
        }

        var result = new List<MemoryEntry>(entries.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = entries.Count - 1; i >= 0; i--) {
            var entry = entries[i];

            if (!seen.Add(entry.Content)) {
                continue;
            }

            // dedup adjacent
            if (result.Count > 0) {
                var prev = result[^1];
                if (prev.Speaker == entry.Speaker && IsContentSimilar(prev.Content, entry.Content)) {
                    if (entry.Content.Length >= prev.Content.Length) {
                        result[^1] = entry;
                    }
                    continue;
                }
            }

            result.Add(entry);
        }

        result.Reverse();
        return result;
    }

    internal static bool IsContentSimilar(string a, string b)
    {
        if (a == b) {
            return true;
        }

        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal)) {
            return true;
        }

        if (a.Length < 3 || b.Length < 3) {
            return false;
        }

        var bigramsA = GetBigrams(a);
        var bigramsB = GetBigrams(b);

        if (bigramsA.Count == 0 || bigramsB.Count == 0) {
            return false;
        }

        var intersection = 0;
        foreach (var bg in bigramsA) {
            if (bigramsB.Contains(bg)) {
                intersection++;
            }
        }

        var union = bigramsA.Count + bigramsB.Count - intersection;
        return union > 0 && (float)intersection / union > 0.5f;

        HashSet<string> GetBigrams(string s)
        {
            var result = new HashSet<string>();
            for (var i = 0; i < s.Length - 1; i++) {
                result.Add(s[i..(i + 2)]);
            }
            return result;
        }
    }
}