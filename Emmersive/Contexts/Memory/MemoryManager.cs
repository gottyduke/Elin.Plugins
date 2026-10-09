using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.API.Plugins;
using Emmersive.API.Services;
using Emmersive.API.ThirdParty;
using Emmersive.ChatProviders;
using Emmersive.Components;
using Emmersive.Helper;
using Emmersive.LangMod;
using Newtonsoft.Json.Linq;

namespace Emmersive.Contexts.Memory;

public sealed class MemoryManager
{
    private const int MaxFactWrapperDepth = 4;

    private int _summarizeInProgress;

    [ElinGameIOProperty("emmersive_memory_store")]
    private static ConcurrentDictionary<int, CharaMemoryStore> Stores
    {
        get => field ??= [];
        set;
    }

    public IReadOnlyList<CharaMemoryStore> AllStores => [..Stores.Values];

    public static MemoryManager Instance => field ??= new();

    public string? LastSummarizeError { get; private set; }

    public int LastSummarizeAdded { get; private set; }

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

        if (!chara.IsPC && !chara.Profile.IsImportant) {
            return;
        }

        var speaker = chara.IsPC ? "Player" : chara.NameSimple;
        Record(chara, speaker, content, chara.turn);

        if (!chara.IsPC) {
            return;
        }

        foreach (var listener in chara.Nearby) {
            if (listener.Profile.IsImportant) {
                Record(listener, speaker, content, chara.turn);
            }
        }
    }

    private void Record(Chara owner, string speaker, string content, int turn)
    {
        var store = GetOrCreate(owner);
        store.AddStm(speaker, content, turn);

        if (store.ShouldSummarize && owner.Profile.AllowSummarize && EmScheduler.IsEnabled) {
            TriggerSummarizeAsync(store, CancellationToken.None).ForgetEx();
        }
    }

    public bool HasRecentTalk(Chara chara, string content, int lookback = 3)
    {
        var store = Get(chara.uid);
        if (store is not { ShortTerm.Count: > 0 }) {
            return false;
        }

        var stm = store.ShortTerm;
        for (var i = stm.Count - 1; i >= 0 && i >= stm.Count - lookback; i--) {
            if (stm[i].Content == content) {
                return true;
            }
        }

        return false;
    }

    public void ClearMemory(Chara chara)
    {
        Stores.TryRemove(chara.uid, out _);
    }

    [ElinPreSave]
    private static void DropBlankFacts(GameIOContext context)
    {
        foreach (var store in Stores.Values) {
            store.LongTerm.RemoveAll(f => f.Fact.IsWhiteSpaceOrNull);
        }
    }

    public async UniTask<bool> TriggerSummarizeAsync(CharaMemoryStore store, CancellationToken ct = default)
    {
        if (!EmScheduler.IsEnabled) {
            LastSummarizeError = "em_ui_scheduler_off".lang();
            return false;
        }

        if (Interlocked.CompareExchange(ref _summarizeInProgress, 1, 0) != 0) {
            LastSummarizeError = "em_ui_sum_busy".lang();
            return false;
        }

        try {
            if (!EmAi.IsAvailable) {
                LastSummarizeError = ApiPoolSelector.Instance.Providers.Count == 0
                    ? "em_ui_no_service".lang()
                    : "em_ui_all_cooling".lang();
                return false;
            }

            var snapshot = store.ShortTerm.Where(e => !e.Summarized).ToList();
            if (snapshot.Count == 0) {
                LastSummarizeError = "em_ui_sum_empty".lang();
                return false;
            }

            var chara = EClass.game.cards.Find(store.Uid);
            if (chara is null) {
                LastSummarizeError = "em_ui_sum_failed".lang();
                return false;
            }

            var recentTalks = string.Join("\n", DeduplicateStm(snapshot).Select(e => e.ToString()));
            var knownFacts = string.Join("; ", store.LongTerm
                .Where(f => !f.Fact.IsWhiteSpaceOrNull)
                .Select(f => f.Fact));

            var prompt = new SystemContext("Emmersive/MemoryPrompt.txt").Build();
            var background = new BackgroundContext(chara).Build();
            var user = $"NPC: {store.Name}\n" +
                       $"Background: {background}\n" +
                       (knownFacts.IsEmptyOrNull ? "" : $"Already known (do not repeat or rephrase): {knownFacts}\n") +
                       $"Recent conversations: {recentTalks}\n" +
                       "Extract key facts:";

            var report = await EmAi.SendWithReportAsync(prompt.ToString(), user, null, ct);
            await UniTask.SwitchToMainThread();

            if (!report.Success) {
                EmMod.Warn<MemoryManager>($"summarization failed for {store.Name}: {report.ErrorReason}");
                LastSummarizeError = report.Status == RequestStatus.Timeout
                    ? "em_ui_sum_timeout".Loc(GetTimeout(report.ProviderId))
                    : "em_ui_sum_failed".lang();
                return false;
            }

            if (report.Content.IsEmptyOrNull) {
                LastSummarizeError = "em_ui_sum_failed".lang();
                return false;
            }

            var facts = ParseFacts(report.Content!);
            if (facts.Count == 0) {
                LastSummarizeError = "em_ui_sum_nothing".lang();
                return false;
            }

            var newFacts = new List<MemoryFact>();
            foreach (var fact in facts) {
                if (!store.LongTerm.Concat(newFacts).Any(f => IsContentSimilar(f.Fact, fact.Fact))) {
                    newFacts.Add(fact);
                }
            }

            store.LongTerm.AddRange(newFacts);
            store.LongTerm.RemoveAll(f => f.Fact.IsEmptyOrNull);
            store.EvictLtm();

            foreach (var entry in snapshot) {
                entry.Summarized = true;
            }

            var keep = EmConfig.Memory.MaxStmEntriesAfterSummarization.Value;
            while (store.ShortTerm.Count > keep && store.ShortTerm[0].Summarized) {
                store.ShortTerm.RemoveAt(0);
            }

            EmMod.Log<MemoryManager>($"summarized {newFacts.Count} facts for {store.Name} " +
                                     $"({report.LatencyMs:F0}ms, {report.TokensInput}+{report.TokensOutput} tokens)");
            LastSummarizeError = null;
            LastSummarizeAdded = newFacts.Count;
        } catch (Exception ex) {
            EmMod.Warn<MemoryManager>($"summarization failed for {store.Name}: {ex.Message}");
            LastSummarizeError = "em_ui_sum_failed".lang();
            return false;
        } finally {
            store.LastSummarized = DateTime.UtcNow;
            Interlocked.Exchange(ref _summarizeInProgress, 0);
        }

        return true;

        static float GetTimeout(string? providerId)
        {
            return ApiPoolSelector.Instance.Providers.FirstOrDefault(p => p.Id == providerId) is ChatProviderBase chat
                ? chat.TimeoutSeconds
                : EmConfig.Policy.Timeout.Value;
        }
    }

    private static List<MemoryFact> ParseFacts(string json)
    {
        json = SceneDirector.StripMarkdownFence(json);
        if (json.IsEmptyOrNull) {
            return [];
        }

        JToken root;
        try {
            root = JToken.Parse(json);
        } catch {
            return [];
            // noexcept
        }

        var array = FindFactArray(root, 0);
        if (array is null) {
            return [];
        }

        var facts = new List<MemoryFact>();
        foreach (var item in array.OfType<JObject>()) {
            var fact = item["fact"]?.ToString().Trim();
            if (fact.IsEmptyOrNull) {
                continue;
            }

            var importance = 1;
            if (item["importance"] is { } imp &&
                float.TryParse(imp.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                importance = Math.Clamp((int)Math.Round(value), 1, 5);
            }

            facts.Add(new() {
                Fact = fact!,
                Importance = importance,
            });
        }

        return facts;
    }

    private static JArray? FindFactArray(JToken token, int depth)
    {
        if (depth > MaxFactWrapperDepth) {
            return null;
        }

        switch (token) {
            case JArray array when array.Any(i => i is JObject obj && obj["fact"] is not null):
                return array;
            case JArray array:
                return array
                    .Select(i => FindFactArray(i, depth + 1))
                    .FirstOrDefault(hit => hit is not null);
            case JObject obj:
                return obj
                    .Properties()
                    .Select(p => FindFactArray(p.Value, depth + 1))
                    .FirstOrDefault(hit => hit is not null);
            default:
                return null;
        }
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

        if (Math.Min(a.Length, b.Length) >= 6 &&
            (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))) {
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