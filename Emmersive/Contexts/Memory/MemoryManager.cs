using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.API.ThirdParty;
using Emmersive.ChatProviders;
using Emmersive.Components;
using Emmersive.Helper;
using Emmersive.LangMod;

namespace Emmersive.Contexts.Memory;

public sealed class MemoryManager
{
    internal const string PlayerSpeaker = "Player";

    private const int MaxHeardTriggers = 256;

    private readonly Dictionary<string, int> _heardTriggers = [];
    private int _recallTick;
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

        if (chara is { IsPC: false, Profile.IsImportant: false }) {
            return;
        }

        var speaker = chara.IsPC ? PlayerSpeaker : chara.NameSimple;
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

        var editing = LayerMemoryEditor.Instance is { Data.Memory: var open } && open == store;
        if (store.ShouldSummarize && owner.Profile.AllowSummarize && EmScheduler.IsEnabled && !editing) {
            TriggerSummarizeAsync(store, CancellationToken.None).ForgetEx();
        }
    }

    public bool HasRecentTalk(Chara chara, string content, int lookback = 6)
    {
        return Get(chara.uid)?.HasRecentRepeat(content, lookback, EClass.world.date.GetRaw()) ?? false;
    }

    internal void MarkSaid(Chara chara, string content)
    {
        if (!EmConfig.Memory.Enabled.Value || chara.IsPC || Get(chara.uid) is not { LongTerm.Count: > 0 } store) {
            return;
        }

        store.MarkSaid(content, IgnoreNames(chara), _recallTick, EmConfig.Memory.LtmRecallCooldown.Value * 2);
    }

    internal int NextRecallTick()
    {
        return ++_recallTick;
    }

    internal bool IsNewTrigger(string text, int tick, int window)
    {
        var key = MemoryRelevance.Normalize(text);
        if (key.Length == 0) {
            return false;
        }

        var fresh = !_heardTriggers.TryGetValue(key, out var last) || tick - last > window;
        _heardTriggers[key] = tick;

        if (_heardTriggers.Count > MaxHeardTriggers) {
            foreach (var old in _heardTriggers.Where(kv => tick - kv.Value > window).Select(kv => kv.Key).ToList()) {
                _heardTriggers.Remove(old);
            }
        }

        return fresh;
    }

    internal static string[] IgnoreNames(Chara chara)
    {
        return chara.IsPC ? [chara.NameSimple] : [chara.NameSimple, EClass.pc.NameSimple];
    }

    public void ClearMemory(Chara chara)
    {
        Stores.TryRemove(chara.uid, out _);
    }

    [ElinPostLoad]
    private static void ClearSession(GameIOContext context)
    {
        Instance._heardTriggers.Clear();
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

            var now = EClass.world.date.GetRaw();
            var recentTalks = string.Join("\n", MemoryContext.BuildTranscript(
                snapshot.Select(e => new TalkLine(e.Speaker, e.Content, e.GameTime, e.Time.Ticks)),
                now));

            List<MemoryFact> known = [..store.LongTerm.Where(f => !f.Fact.IsWhiteSpaceOrNull)];
            List<string> knownTexts = [..known.Select(f => f.Fact)];
            var knownFacts = known.Count == 0
                ? "-"
                : string.Join("\n", known.Select((f, i) => $"{i + 1}. {f.Fact}"));

            var prompt = new SystemContext("Emmersive/MemoryPrompt.txt").Build();
            var user = RequestTemplate()
                .Replace("{{npc}}", store.Name)
                .Replace("{{background}}", new BackgroundContext(chara).Build()?.ToString())
                .Replace("{{facts}}", knownFacts)
                .Replace("{{talks}}", recentTalks);

            var report = await EmAi.SendWithReportAsync(prompt.ToString(), user, null, ct);
            await UniTask.SwitchToMainThread();

            if (!report.Success) {
                EmMod.Warn<MemoryManager>($"summarization failed for {store.Name}: {report.ErrorReason}");
                LastSummarizeError = report.Status == RequestStatus.Timeout
                    ? "em_ui_sum_timeout".Loc(GetTimeout(report.ProviderId))
                    : "em_ui_sum_failed".lang();
                return false;
            }

            if (!Stores.TryGetValue(store.Uid, out var current) || !ReferenceEquals(current, store)) {
                LastSummarizeError = "em_ui_sum_failed".lang();
                return false;
            }

            if (report.Content.IsEmptyOrNull || MemoryConsolidation.Parse(report.Content!) is not { } ops) {
                EmMod.Warn<MemoryManager>($"summarization reply for {store.Name} is not usable\n{report.Content}");
                LastSummarizeError = "em_ui_sum_failed".lang();
                return false;
            }

            var changes = MemoryConsolidation.Apply(store, known, ops, now, knownTexts);
            var forgotten = store.EvictLtm(EmConfig.Memory.MaxLtmEntries.Value, DateTime.UtcNow);

            foreach (var entry in snapshot) {
                entry.Summarized = true;
            }

            var keep = EmConfig.Memory.MaxStmEntriesAfterSummarization.Value;
            while (store.ShortTerm.Count > keep && store.ShortTerm[0].Summarized) {
                store.ShortTerm.RemoveAt(0);
            }

            PublishFactChanges(store, chara, changes, forgotten);

            var kept = changes.Count(c => !forgotten.Contains(c.Fact));
            EmMod.Log<MemoryManager>($"summarized {kept} facts for {store.Name}, forgot {forgotten.Count} " +
                                     $"({report.LatencyMs:F0}ms, {report.TokensInput}+{report.TokensOutput} tokens)");

            if (ops.Count == 0) {
                LastSummarizeError = "em_ui_sum_nothing".lang();
                return false;
            }

            LastSummarizeError = null;
            LastSummarizeAdded = kept;
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

        static string RequestTemplate()
        {
            const string key = "Emmersive/MemoryRequest.txt";
            var template = new SystemContext(key).Build().ToString();
            if (template.Contains("{{facts}}") && template.Contains("{{talks}}")) {
                return template;
            }

            EmMod.Warn<MemoryManager>($"{key} needs {{{{facts}}}} and {{{{talks}}}}, using the built-in one");
            return ResourceFetch.GetDefaultResource(key);
        }
    }

    private static void PublishFactChanges(CharaMemoryStore store,
                                           Chara chara,
                                           List<FactChange> changes,
                                           List<MemoryFact> forgotten)
    {
        var added = changes
            .Where(c => c.Kind == MemoryFactChange.Added)
            .Select(c => c.Fact)
            .ToHashSet();

        var events = changes
            .Where(c => !(c.Kind == MemoryFactChange.Added && forgotten.Contains(c.Fact)))
            .Concat(forgotten
                .Where(f => !added.Contains(f))
                .Select(f => new FactChange(MemoryFactChange.Forgotten, f, null, 0)));

        foreach (var change in events) {
            if (change.Fact.Fact.IsWhiteSpaceOrNull) {
                continue;
            }

            switch (change.Kind) {
                case MemoryFactChange.Updated:
                    EmMod.Log<MemoryManager>($"{store.Name} fact rewritten: {change.OldFact} -> {change.Fact.Fact}");
                    break;
                case MemoryFactChange.Forgotten:
                    EmMod.Log<MemoryManager>($"{store.Name} fact forgotten: {change.Fact.Fact}");
                    break;
            }

            EmEvent.RaiseMemoryFactChanged(new() {
                Uid = store.Uid,
                Name = store.Name,
                Change = change.Kind,
                Fact = change.Fact.Fact,
                Importance = change.Fact.Importance,
                OldFact = change.OldFact,
                OldImportance = change.OldImportance,
                Chara = chara,
            });
        }
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