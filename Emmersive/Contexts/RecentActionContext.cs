using System;
using System.Collections.Generic;
using System.Linq;
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

            if (IsTalkEntry(text)) {
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
        return logs;
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
    }
}