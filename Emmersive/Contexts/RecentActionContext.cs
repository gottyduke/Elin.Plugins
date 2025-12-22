using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class RecentActionContext : ContextProviderBase
{
    private const int BufSize = 1024;

    internal static readonly List<(string actor, string text)> RecentActions = [];

    private static int _indexSinceStart;
    private static readonly string _comma = "_comma".lang();
    private static readonly string _push = "em_push".lang();

    internal static HashSet<string> Filters = [_push];

    public override string Name => "recent_action_log";

    public override object? Build()
    {
        List<string> actions;
        var depth = EmConfig.Context.RecentLogDepth.Value;
        var session = RecentActions.TakeLast(depth);

        if (EmConfig.Context.RecentTalkOnly.Value) {
            actions = session
                .Select(tl => $"{tl.actor}: {tl.text}")
                .ToList();
        } else {
            actions = [];

            var recentDict = RecentActions
                .TakeLast(depth)
                .ToLookup(a => a.text);

            var logs = ExtractUniqueLog(depth);

            foreach (var raw in logs) {
                var sanitized = raw;

                if (sanitized.Length > 2 && sanitized.StartsWith('"') && sanitized.EndsWith('"')) {
                    sanitized = sanitized[1..^1];
                }

                var entry = recentDict[sanitized].LastOrDefault();
                if (entry.actor is { } actor) {
                    sanitized = $"{actor}: {sanitized}";
                }

                actions.Add(sanitized);
            }
        }

        return actions.Count == 0 ? null : actions;
    }

    public static void Add(string actor, string entry)
    {
        Filters.Remove("");

        if (Filters.Any(entry.Contains)) {
            return;
        }

        entry = entry.Trim().Trim('"');

        if (actor == EClass.pc.NameSimple) {
            actor = "you".lang().ToTitleCase();
        }

        if (HasDuplicate(actor, entry, 1)) {
            return;
        }

        RecentActions.Add((actor, entry));

        var trim = Math.Max(BufSize, EmConfig.Context.RecentLogDepth.Value * 2);
        if (RecentActions.Count >= trim) {
            TrimExcess(trim);
        }
    }

    public static bool HasDuplicate(string actor, string entry, int depth = -1)
    {
        if (RecentActions.Count == 0) {
            return false;
        }

        if (depth == -1) {
            depth = EmConfig.Context.RecentLogDepth.Value;
        }

        return RecentActions
            .TakeLast(depth)
            .Any(a => a.actor == actor && a.text == entry);
    }

    public static List<string> ExtractUniqueLog(int depth)
    {
        var fullLog = EClass.game.log;
        var dict = fullLog.dict;
        var lastIndex = fullLog.currentLogIndex - 1;

        var logs = new List<string>(depth);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? current = null;

        Filters.Remove("");

        while (lastIndex >= _indexSinceStart && logs.Count < depth) {
            if (!dict.TryGetValue(lastIndex, out var msg)) {
                break;
            }

            var text = msg.text.StripBrackets();
            if (text.IsEmptyOrNull || Filters.Any(text.Contains)) {
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

    public static void TrimExcess(int bufSize)
    {
        RecentActions.RemoveRange(0, RecentActions.Count - bufSize);
    }

    [CwlPostLoad]
    public static void ClearSession()
    {
        RecentActions.Clear();
        _indexSinceStart = EClass.game.log.currentLogIndex - 1;

        if (ResourceFetch.Context.Load<HashSet<string>>(out var filters, "action_filters")) {
            Filters = filters;
        }
    }

    [CwlPostSave]
    public static void SaveFilters()
    {
        Filters.Add(_push);
        ResourceFetch.Context.Save(Filters, "action_filters");
    }
}