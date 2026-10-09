using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;
using TalkLine = (string Speaker, string Content, int Time);

namespace Emmersive.Contexts.Memory;

public sealed class MemoryContext(IReadOnlyList<Chara> charas, HashSet<string>? excludedEntries = null) : ContextProviderBase
{
    private readonly HashSet<string> _excludedEntries = excludedEntries ?? [];

    public override string Name => "npc_memories";

    protected override IDictionary<string, object>? BuildInternal()
    {
        if (charas.Count == 0) {
            return null;
        }

        var logs = ReadGameLogTalks();

        var result = new Dictionary<string, object>();
        var hasAny = false;

        foreach (var chara in charas) {
            var store = MemoryManager.Instance.Get(chara.uid);
            var memory = new Dictionary<string, object>();

            var rawTalks = new List<TalkLine>();
            var sentEntries = new List<MemoryEntry>();

            // stm from memory store
            if (store is { ShortTerm.Count: > 0 }) {
                var stm = store.GetRecentStm();
                foreach (var entry in stm) {
                    if (_excludedEntries.Contains(entry.Content)) {
                        continue;
                    }
                    rawTalks.Add((entry.Speaker, entry.Content, entry.GameTime));
                    sentEntries.Add(entry);
                }
            }

            // stm from game log talk entries
            var stmLogged = store?.ShortTerm
                .Select(e => e.Content)
                .ToHashSet(StringComparer.Ordinal) ?? [];

            var room = EmConfig.Memory.MaxStmInContext.Value - rawTalks.Count;
            if (room > 0 && logs.TryGetValue(chara.NameSimple, out var logTalks)) {
                rawTalks.AddRange(logTalks
                    .Where(t => !_excludedEntries.Contains(t.Content) && !stmLogged.Contains(t.Content))
                    .Reverse()
                    .Take(room)
                    .Reverse());
            }

            // dedup
            var deduped = DeduplicateTalkList([..rawTalks.OrderBy(t => t.Time)]);
            if (deduped.Count > 0) {
                memory["recent_talks"] = deduped
                    .Select(t => $"[{t.Speaker}]: {t.Content}")
                    .ToList();
            }

            // ltm facts
            if (store is { LongTerm.Count: > 0 }) {
                var ltm = store.GetTopLtm();
                if (ltm.Count > 0) {
                    memory["known_facts"] = ltm.Select(f => f.ToString()).ToList();
                }
            }

            if (memory.Count > 0) {
                result[chara.NameSimple] = memory;
                hasAny = true;
            }

            // mark sent so they won't repeat
            foreach (var entry in sentEntries) {
                entry.MarkSent();
            }
        }

        return hasAny ? result : null;
    }

    private static List<TalkLine> DeduplicateTalkList(List<TalkLine> talks)
    {
        var result = new List<TalkLine>(talks.Count);

        // recent
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

            talks.Add((speaker, content, msg.date?.GetRaw() ?? 0));
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