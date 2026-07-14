using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.API.Services;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class RecentActionContext : ContextProviderBase
{
    private static int _indexSinceStart;
    private static readonly string _comma = "_comma".lang();
    private static readonly string _push = "em_push".lang();

    internal static HashSet<string> Filters = [_push];

    public override string Name => "recent_action_log";

    public override object? Build()
    {
        var depth = EmConfig.Context.GameLogDepth.Value;
        if (depth <= 0) {
            return null;
        }

        var logs = ExtractGameEvents(depth);
        return logs.Count == 0 ? null : logs;
    }

    public static List<string> ExtractGameEvents(int depth)
    {
        var fullLog = EClass.game.log;
        var dict = fullLog.dict;
        var lastIndex = fullLog.currentLogIndex - 1;

        var logs = new List<string>(depth);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var memoryEnabled = EmConfig.Memory.Enabled.Value;
        string? current = null;

        while (lastIndex >= _indexSinceStart && logs.Count < depth) {
            if (!dict.TryGetValue(lastIndex, out var msg)) {
                break;
            }

            var text = msg.text.StripBrackets();
            if (text.IsEmptyOrNull || Filters.Any(text.Contains)) {
                lastIndex--;
                continue;
            }

            // memory talks are separated
            if (memoryEnabled && IsTalkEntry(text)) {
                lastIndex--;
                continue;
            }

            current = text + (current ?? "");
            if (!text.StartsWith(" ") && !text.StartsWith(_comma)) {
                if (seen.Add(current)) {
                    logs.Add(current);
                }

                current = null;
            }

            lastIndex--;
        }

        logs.Reverse();

        if (!memoryEnabled) {
            logs = DeduplicateGameLogs(logs);
        }

        return logs;
    }

    private static List<string> DeduplicateGameLogs(List<string> logs)
    {
        if (logs.Count <= 1) {
            return logs;
        }

        var result = new List<string>(logs.Count);
        for (var i = 0; i < logs.Count; i++) {
            var current = logs[i];
            var currentIsTalk = TryParseTalkEntry(current, out var curSpeaker, out var curContent);

            if (result.Count > 0 && currentIsTalk) {
                var prev = result[^1];
                var prevIsTalk = TryParseTalkEntry(prev, out var prevSpeaker, out var prevContent);

                if (prevIsTalk &&
                    string.Equals(prevSpeaker, curSpeaker, StringComparison.Ordinal) &&
                    MemoryManager.IsContentSimilar(prevContent, curContent)) {
                    if (curContent.Length >= prevContent.Length) {
                        result[^1] = current;
                    }
                    continue;
                }
            }

            result.Add(current);
        }

        return result;
    }

    private static bool TryParseTalkEntry(string text, out string speaker, out string content)
    {
        speaker = "";
        content = "";

        var colonIdx = text.IndexOf(": ", StringComparison.Ordinal);
        if (colonIdx <= 0) {
            return false;
        }

        speaker = text[..colonIdx];
        content = text[(colonIdx + 2)..];
        return !speaker.IsEmptyOrNull && !content.IsEmptyOrNull;
    }

    private static bool IsTalkEntry(string text)
    {
        return text.Contains(": ") &&
               (text.StartsWith('"') || char.IsLetter(text[0]));
    }

    [ElinPostLoad]
    public static void ClearSession(GameIOContext context)
    {
        _indexSinceStart = EClass.game.log.currentLogIndex - 1;

        if (ResourceFetch.Context.Load<HashSet<string>>("action_filters", out var filters)) {
            Filters = filters;
        }
    }

    [ElinPostSave]
    public static void SaveFilters(GameIOContext context)
    {
        Filters.Add(_push);
        ResourceFetch.Context.Save("action_filters", Filters);
    }
}