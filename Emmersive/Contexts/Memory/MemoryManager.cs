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
        var store = GetOrCreate(chara);
        var speaker = chara.IsPC ? "Player" : chara.NameSimple;
        store.AddStm(speaker, content, chara.turn);

        if (store.ShouldSummarize) {
            TriggerSummarizeAsync(store).ForgetEx();
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

    public async UniTask<bool> TriggerSummarizeAsync(CharaMemoryStore store)
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

            var recentTalks = string.Join("\n", stmCopy.Select(e => e.ToString()));
            var system = "You are a memory summarizer for an NPC in a fantasy game. " +
                         "Given recent conversation history with this NPC, extract 1-3 key facts " +
                         "about the relationship, events, character traits, or significant revelations. " +
                         "De-duplicate repetitive talks before summarization. " +
                         "Output ONLY a JSON array: [{\"fact\":\"...\",\"importance\":1-5}]. " +
                         "Importance: 1=trivial, 3=notable, 5=crucial character-defining. " +
                         "Focus on persistent knowledge, not transient chitchat.";

            var user = $"NPC: {store.Name}\n\nRecent conversations:\n{recentTalks}\n\nExtract key facts:";

            var response = await EmAi.SendAsync(system, user);
            if (response.IsEmptyOrNull) {
                return false;
            }

            var facts = ParseFacts(response);
            if (facts?.Count is > 0) {
                store.LongTerm.AddRange(facts);
                store.LongTerm.RemoveAll(f => f.Fact.IsEmptyOrNull);
                store.LastSummarized = DateTime.UtcNow;

                var keepCount = Math.Min(3, store.ShortTerm.Count);
                store.ShortTerm.RemoveRange(0, store.ShortTerm.Count - keepCount);

                EmMod.Log<MemoryManager>($"summarized {facts.Count} facts for {store.Name}");
            }
        } catch (Exception ex) {
            EmMod.Warn<MemoryManager>($"summarization failed for {store.Name}: {ex.Message}");
            return false;
            // noexcept
        } finally {
            Interlocked.Exchange(ref _summarizeInProgress, 0);
        }

        return true;
    }

    private static List<MemoryFact>? ParseFacts(string json)
    {
        var raw = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json.Trim());

        return raw?.Select(item => new MemoryFact {
                Fact = item.TryGetValue("fact", out var f) ? f?.ToString() ?? "" : "",
                Importance = item.TryGetValue("importance", out var i) && int.TryParse(i?.ToString(), out var iv)
                    ? Math.Clamp(iv, 1, 5)
                    : 1,
            })
            .Where(f => !f.Fact.IsEmptyOrNull)
            .ToList();
    }
}