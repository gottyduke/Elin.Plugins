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

            // stm from memory store
            var stmMemory = new List<string>();
            if (store is { ShortTerm.Count: > 0 }) {
                var stm = store.GetRecentStm();
                foreach (var entry in stm) {
                    if (_excludedEntries.Contains(entry.Content)) {
                        continue;
                    }
                    stmMemory.Add(entry.ToString());
                }
            }

            // stm from game log talk entries
            var stmLogged = store?.ShortTerm
                .Select(e => e.Content)
                .ToHashSet(StringComparer.Ordinal) ?? [];

            if (logs.TryGetValue(chara.NameSimple, out var talks)) {
                foreach (var talk in talks) {
                    if (_excludedEntries.Contains(talk.Content) || stmLogged.Contains(talk.Content)) {
                        continue;
                    }
                    stmMemory.Add($"[{talk.Speaker}]: {talk.Content}");
                }
            }

            if (stmMemory.Count > 0) {
                memory["recent_talks"] = stmMemory;
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
        }

        return hasAny ? result : null;
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

        foreach (var (_, talks) in result) {
            talks.Reverse();
        }

        return result;
    }
}