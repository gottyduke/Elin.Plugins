using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Components;
using Emmersive.Helper;

namespace Emmersive.Contexts.Memory;

internal readonly record struct TalkLine(string Speaker, string Content, int Time, long Order);

internal sealed record TalkSelection(
    List<TalkLine> Talks,
    List<MemoryEntry> Sent,
    List<MemoryEntry> Covered,
    List<(string Speaker, string Content)> Unseen);

public sealed class MemoryContext : ContextProviderBase
{
    private const int MinutesPerHour = 60;
    private const int MinutesPerDay = 1440;

    private readonly IReadOnlyList<Chara> _charas;
    private readonly IReadOnlyList<(string? Speaker, string Text)> _triggers;

    public MemoryContext(IReadOnlyList<Chara> charas, HashSet<string>? excludedEntries = null)
    {
        _charas = charas;
        _triggers = [..(excludedEntries ?? []).Select(text => ((string?)null, text))];
    }

    public MemoryContext(IReadOnlyList<Chara> charas, IReadOnlyList<SceneTriggerEvent> triggers)
    {
        _charas = charas;
        _triggers = [
            ..triggers
                .Where(t => !t.Trigger.IsEmptyOrNull)
                .Select(t => ((string?)SpeakerOf(t.Chara), t.Trigger)),
        ];
    }

    public override string Name => "npc_memories";

    protected override IDictionary<string, object>? BuildInternal()
    {
        if (_charas.Count == 0) {
            return null;
        }

        var manager = MemoryManager.Instance;
        var now = EClass.world.date.GetRaw();
        var tick = manager.NextRecallTick();

        var views = _charas
            .Select(c => (Chara: c, Store: manager.Get(c.uid)))
            .ToList();

        var selection = SelectTalks(
            [..views.Select(v => (v.Store, v.Chara.IsPC ? null : v.Chara.NameSimple, v.Chara.NameSimple))],
            _triggers,
            ReadGameLogTalks(),
            EmConfig.Memory.MaxStmInContext.Value,
            EmConfig.Memory.MaxStmRepeatInContext.Value,
            now);

        var result = new Dictionary<string, object>();

        var transcript = BuildTranscript(selection.Talks, now);
        if (transcript.Count > 0) {
            result["recent_talks"] = transcript;
        }

        var facts = RecallFacts(views, selection.Unseen, tick, now);
        if (facts.Count > 0) {
            result["known_facts"] = facts;
        }

        if (result.Count == 0) {
            return null;
        }

        var usage = new SystemContext("Emmersive/MemoryUsage.txt").Build().ToString();
        if (!usage.IsWhiteSpaceOrNull) {
            result["usage"] = usage;
        }

        foreach (var entry in selection.Sent) {
            entry.MarkSent();
            entry.Seen = true;
        }

        foreach (var entry in selection.Covered) {
            entry.Seen = true;
        }

        return result;
    }

    internal static List<string> BuildTranscript(IEnumerable<TalkLine> talks, int now)
    {
        return [
            ..DeduplicateTalks([..talks.OrderBy(t => t.Time).ThenBy(t => t.Order)])
                .Select(t => FormatTalk(t.Speaker, t.Content, t.Time, now)),
        ];
    }

    internal static string FormatTalk(string speaker, string content, int time, int now)
    {
        var elapsed = now - time;
        var age = "";
        if (time > 0 && elapsed >= MinutesPerHour) {
            try {
                age = elapsed < MinutesPerDay
                    ? string.Format(MemoryLexicon.Current.HoursAgo, elapsed / MinutesPerHour)
                    : string.Format(MemoryLexicon.Current.DaysAgo, elapsed / MinutesPerDay);
            } catch (FormatException) {
                // noexcept
            }
        }

        return $"[{speaker}]{age}: {content}";
    }

    private static List<TalkLine> DeduplicateTalks(List<TalkLine> talks)
    {
        var result = new List<TalkLine>(talks.Count);

        for (var i = talks.Count - 1; i >= 0; i--) {
            var talk = talks[i];
            var similar = result.FindIndex(t => t.Content == talk.Content ||
                                                (t.Speaker == talk.Speaker &&
                                                 MemoryManager.IsContentSimilar(t.Content, talk.Content)));
            if (similar < 0) {
                result.Add(talk);
            } else if (talk.Content.Length > result[similar].Content.Length) {
                result[similar] = talk;
            }
        }

        result.Reverse();
        return result;
    }

    internal static TalkSelection SelectTalks(IReadOnlyList<(CharaMemoryStore? Store, string? Owner, string LogName)> stores,
                                              IReadOnlyList<(string? Speaker, string Text)> triggers,
                                              IReadOnlyDictionary<string, List<TalkLine>> logs,
                                              int maxStm,
                                              int maxRepeat,
                                              int now)
    {
        var selection = new TalkSelection([], [], [], []);
        var normalizedTriggers = triggers
            .Select(t => (t.Speaker, Text: MemoryRelevance.Normalize(t.Text)))
            .Where(t => t.Text.Length > 0)
            .ToList();

        var stmLogged = stores
            .SelectMany(s => s.Store?.ShortTerm ?? [])
            .Select(e => e.Content)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (store, owner, logName) in stores) {
            var taken = 0;

            if (store is { ShortTerm.Count: > 0 }) {
                foreach (var entry in store.GetRecentStm(maxStm, maxRepeat, owner, now)) {
                    if (IsTriggerLine(entry.Speaker, entry.Content, !entry.Seen)) {
                        selection.Covered.Add(entry);
                        continue;
                    }

                    selection.Talks.Add(new(entry.Speaker, entry.Content, entry.GameTime, entry.Time.Ticks));
                    selection.Sent.Add(entry);
                    taken++;

                    if (!entry.Seen) {
                        selection.Unseen.Add((entry.Speaker, entry.Content));
                    }
                }
            }

            var room = maxStm - taken;
            if (room > 0 && logs.TryGetValue(logName, out var logTalks)) {
                selection.Talks.AddRange(logTalks
                    .Where(t => !stmLogged.Contains(t.Content) && !IsTriggerLine(t.Speaker, t.Content, false))
                    .Reverse()
                    .Take(room)
                    .Reverse());
            }
        }

        return selection;

        bool IsTriggerLine(string speaker, string content, bool part)
        {
            var normalized = MemoryRelevance.Normalize(content);
            return normalized.Length > 0 &&
                   normalizedTriggers.Any(t => (t.Speaker is null || t.Speaker == speaker) &&
                                               (part ? t.Text.Contains(normalized) : t.Text == normalized));
        }
    }

    internal static (List<string> Lines, List<string> PlayerLines) RecallLines(
        string own,
        IReadOnlyList<(string? Speaker, string Text)> freshTriggers,
        IReadOnlyList<(string Speaker, string Content)> unseen)
    {
        var heard = freshTriggers
            .Concat(unseen.Select(u => ((string?)u.Speaker, u.Content)))
            .Where(l => l.Item1 != own)
            .ToList();

        return (
            [..heard.Select(l => l.Item2).Distinct()],
            [..heard.Where(l => l.Item1 == MemoryManager.PlayerSpeaker).Select(l => l.Item2).Distinct()]);
    }

    private Dictionary<string, object> RecallFacts(List<(Chara Chara, CharaMemoryStore? Store)> views,
                                                   List<(string Speaker, string Content)> unseen,
                                                   int tick,
                                                   int now)
    {
        var facts = new Dictionary<string, object>(StringComparer.Ordinal);
        if (!views.Any(v => v.Store is { LongTerm.Count: > 0 })) {
            return facts;
        }

        var max = EmConfig.Memory.MaxLtmInContext.Value;
        var cooldown = EmConfig.Memory.LtmRecallCooldown.Value;
        var freshTriggers = FreshTriggers(_triggers, MemoryManager.Instance, tick, cooldown * 2);

        var tokens = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var duplicateNames = views
            .GroupBy(v => v.Chara.NameSimple)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var (chara, store) in views) {
            if (store is not { LongTerm.Count: > 0 }) {
                continue;
            }

            var (lines, playerLines) = RecallLines(SpeakerOf(chara), freshTriggers, unseen);
            var request = new RecallRequest(
                [..lines.Select(Tokens)],
                MemoryRelevance.IgnoreTokens(MemoryManager.IgnoreNames(chara)),
                tick,
                max,
                cooldown,
                now,
                [..playerLines.Select(Tokens)]);

            var recalled = store.RecallLtm(request);
            if (recalled.Count == 0) {
                continue;
            }

            var key = duplicateNames.Contains(chara.NameSimple)
                ? $"{chara.NameSimple}#{chara.uid}"
                : chara.NameSimple;
            facts[key] = recalled.Select(f => f.Fact).ToList();
        }

        return facts;

        HashSet<string> Tokens(string line)
        {
            if (!tokens.TryGetValue(line, out var set)) {
                set = MemoryRelevance.Tokenize(line);
                tokens[line] = set;
            }

            return set;
        }
    }

    internal static List<(string? Speaker, string Text)> FreshTriggers(IReadOnlyList<(string? Speaker, string Text)> triggers,
                                                                       MemoryManager manager,
                                                                       int tick,
                                                                       int window)
    {
        return [
            ..triggers.Where(t => manager.IsNewTrigger(t.Text, tick, window) || t.Speaker == MemoryManager.PlayerSpeaker),
        ];
    }

    private static string SpeakerOf(Chara chara)
    {
        return chara.IsPC ? MemoryManager.PlayerSpeaker : chara.NameSimple;
    }

    private static Dictionary<string, List<TalkLine>> ReadGameLogTalks()
    {
        var depth = EmConfig.Context.GameLogDepth.Value;
        var result = new Dictionary<string, List<TalkLine>>(StringComparer.Ordinal);

        if (depth <= 0) {
            return result;
        }

        var fullLog = EClass.game.log;
        var dict = fullLog.dict;
        var lastIndex = fullLog.currentLogIndex - 1;
        var scanned = 0;

        while (lastIndex >= 0 && scanned < depth) {
            if (!dict.TryGetValue(lastIndex, out var msg)) {
                break;
            }

            var text = msg.text.StripBrackets();
            lastIndex--;

            if (text.IsEmptyOrNull) {
                continue;
            }

            var colonIdx = text.IndexOf(": ", StringComparison.Ordinal);
            if (colonIdx <= 0) {
                continue;
            }

            var speaker = text[..colonIdx];
            var content = text[(colonIdx + 2)..];

            if (speaker.IsEmptyOrNull || content.IsEmptyOrNull) {
                continue;
            }

            if (!result.TryGetValue(speaker, out var talks)) {
                talks = [];
                result[speaker] = talks;
            }

            talks.Add(new(speaker, content, msg.date?.GetRaw() ?? 0, 0));
            scanned++;
        }

        // dedup adjacent
        foreach (var (_, talks) in result) {
            talks.Reverse();
            for (var i = 0; i < talks.Count - 1;) {
                if (MemoryManager.IsContentSimilar(talks[i].Content, talks[i + 1].Content)) {
                    talks.RemoveAt(i);
                } else {
                    i++;
                }
            }
        }

        return result;
    }
}