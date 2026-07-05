using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;

namespace Emmersive.Contexts.Memory;

public sealed class MemoryContext(HashSet<string>? excludedEntries) : ContextProviderBase
{
    private readonly HashSet<string> _excludedEntries = excludedEntries ?? [];

    public MemoryContext() : this(null) { }

    public override string Name => "npc_memories";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var nearbyCharas = PointScan.LastNearby.ToList();
        nearbyCharas.Add(EClass.pc);
        if (nearbyCharas.Count == 0) {
            return null;
        }

        var logs = ReadGameLogTalks();

        var result = new Dictionary<string, object>();
        var hasAny = false;

        foreach (var chara in nearbyCharas) {
            var store = MemoryManager.Instance.Get(chara.uid);
            var memory = new Dictionary<string, object>();

            var rawTalks = new List<(string Speaker, string Content)>();
            var sentEntries = new List<MemoryEntry>();

            // stm from memory store
            if (store is { ShortTerm.Count: > 0 }) {
                var stm = store.GetRecentStm();
                foreach (var entry in stm) {
                    if (_excludedEntries.Contains(entry.Content)) {
                        continue;
                    }
                    rawTalks.Add((entry.Speaker, entry.Content));
                    sentEntries.Add(entry);
                }
            }

            // stm from game log talk entries
            var stmLogged = store?.ShortTerm
                .Select(e => e.Content)
                .ToHashSet(StringComparer.Ordinal) ?? [];

            if (rawTalks.Count < EmConfig.Memory.MaxStmInContext.Value &&
                logs.TryGetValue(chara.NameSimple, out var logTalks)) {
                foreach (var talk in logTalks) {
                    if (_excludedEntries.Contains(talk.Content) || stmLogged.Contains(talk.Content)) {
                        continue;
                    }
                    rawTalks.Add((talk.Speaker, talk.Content));
                    if (rawTalks.Count >= EmConfig.Memory.MaxStmInContext.Value) {
                        break;
                    }
                }
            }

            // dedup
            var deduped = DeduplicateTalkList(rawTalks);
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

    private static List<(string Speaker, string Content)> DeduplicateTalkList(
        List<(string Speaker, string Content)> talks)
    {
        if (talks.Count <= 1) {
            return talks;
        }

        var result = new List<(string Speaker, string Content)>(talks.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // recent
        for (var i = talks.Count - 1; i >= 0; i--) {
            var talk = talks[i];

            if (!seen.Add(talk.Content)) {
                continue;
            }

            if (result.Count > 0) {
                var prev = result[^1];
                if (prev.Speaker == talk.Speaker &&
                    MemoryManager.IsContentSimilar(prev.Content, talk.Content)) {
                    if (talk.Content.Length >= prev.Content.Length) {
                        result[^1] = talk;
                    }
                    continue;
                }
            }

            result.Add(talk);
        }

        result.Reverse();
        return result;
    }

    private static Dictionary<string, List<(string Speaker, string Content)>> ReadGameLogTalks()
    {
        var depth = EmConfig.Context.GameLogDepth.Value;
        var result = new Dictionary<string, List<(string Speaker, string Content)>>(StringComparer.Ordinal);

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

            talks.Add((speaker, content));
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