using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;

namespace Emmersive.Contexts;

public class RecentActionContext : ContextProviderBase
{
    private const int BufSize = 1024;

    internal static readonly List<(string actor, string text)> RecentActions = [];
    private static int _indexSinceStart;
    public override string Name => "recent_action_log";

    public override object? Build()
    {
        List<string> actions;
        var depth = EmConfig.Context.RecentLogDepth.Value;
        var session = RecentActions.TakeLast(depth);

        if (EmConfig.Context.RecentTalkOnly.Value) {
            lock (RecentActions) {
                actions = session
                    .Select(tl => $"{tl.actor}: {tl.text}")
                    .ToList();
            }
        } else {
            actions = [];

            var sanitizedSession = session.ToArray();
            foreach (var log in ExtractUniqueLog(depth)) {
                var sanitized = log;

                if (sanitized.StartsWith('"') && sanitized.EndsWith('"') && sanitized.Length > 2) {
                    sanitized = sanitized[1..^1];
                }

                foreach (var (actor, talk) in sanitizedSession) {
                    if (sanitized != talk) {
                        continue;
                    }

                    sanitized = $"{actor}: {talk}";
                    break;
                }

                actions.Add(sanitized);
            }
        }

        return actions.Count == 0
            ? null
            : actions;
    }

    public static void Add(string actor, string entry)
    {
        entry = entry.Trim();

        if (actor == EClass.pc.NameSimple) {
            actor = "you".lang().ToTitleCase();
        }

        if (RecentActions.Count > 0) {
            if (RecentActions[^1].actor == entry && RecentActions[^1].text == entry) {
                return;
            }
        }

        RecentActions.Add((actor, entry));

        var trim = Math.Max(BufSize, EmConfig.Context.RecentLogDepth.Value * 2);
        if (RecentActions.Count >= trim) {
            TrimExcess(trim);
        }
    }

    public static List<string> ExtractUniqueLog(int depth)
    {
        var fullLog = EClass.game.log;
        var lastIndex = fullLog.currentLogIndex - 1;
        var dict = fullLog.dict;

        lock (dict) {
            HashSet<string> seen = [];
            List<string> logs = [];

            var appendNext = false;
            var suffix = "";
            while (lastIndex >= _indexSinceStart) {
                if (!dict.TryGetValue(lastIndex, out var msg)) {
                    break;
                }

                var log = msg.text;
                if (log.StartsWith(" ")) {
                    appendNext = true;
                    suffix = log;
                    continue;
                }

                if (seen.Add(log)) {
                    if (appendNext) {
                        log += suffix;
                        appendNext = false;
                    }

                    logs.Add(log);

                    if (logs.Count >= depth) {
                        break;
                    }
                }

                lastIndex--;
            }

            logs.Reverse();

            return logs;
        }
    }

    public static void TrimExcess(int bufSize)
    {
        RecentActions.RemoveRange(0, RecentActions.Count - bufSize);
    }

    [CwlPostLoad]
    public static void ClearSession()
    {
        RecentActions.Clear();
        lock (EClass.game.log) {
            _indexSinceStart = EClass.game.log.currentLogIndex - 1;
        }
    }
}