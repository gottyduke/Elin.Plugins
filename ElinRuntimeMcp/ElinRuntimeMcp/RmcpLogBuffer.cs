using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;

namespace ElinRuntimeMcp;

/// <summary>
///     Keeps the latest log events in memory so the bridge can read them per instance, including sandboxed ones.
///     Sees everything BepInEx's LogOutput.log sees: plugin loggers, and Unity's Debug.Log while UnityLogListening is on (the default).
///     Starts with a copy of Unity's log file, so mod-loader lines from before this mod loaded are included.
///     Thread-safe; the listener serves reads straight from the socket thread, so logs stay readable while the main thread hangs.
/// </summary>
internal sealed class RmcpLogBuffer : ILogListener
{
    internal const int Capacity = 5000;

    // "[Warning:  BepInEx] message", the shape BepInEx writes its lines in (LogEventArgs.ToString);
    // only real level names, so Unity's own "[Physics::Module] ..." stays a plain line
    private static readonly Regex _prefixed = new(@"^\[(Fatal|Error|Warning|Message|Info|Debug)\s*:\s*(.*?)\] (.*)$");

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private static readonly object _lock = new();

    // startup lines are kept apart from the ring so a long session cannot push the mod loader's verdicts out
    private static readonly List<Entry> _startup = [];
    private static readonly Queue<Entry> _entries = [];
    private static long _seq;

    public LogLevel LogLevelFilter => LogLevel.All;

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        Add(new(DateTime.Now, eventArgs.Level.ToString(), eventArgs.Source?.SourceName ?? "?", eventArgs.Data?.ToString() ?? ""));
    }

    public void Dispose()
    {
    }

    internal static void Install()
    {
        // copy first, then listen: anything logged in between would otherwise be read twice
        Backfill();
        Logger.Listeners.Add(new RmcpLogBuffer());
    }

    private static void Add(Entry entry)
    {
        lock (_lock) {
            entry.Seq = ++_seq;
            if (entry.Time is null) {
                _startup.Add(entry);
                return;
            }

            _entries.Enqueue(entry);
            if (_entries.Count > Capacity) {
                _entries.Dequeue();
            }
        }
    }

    /// <summary>
    ///     Copies Unity's own log file (Player.log) in: it is the one place that holds both the game's Debug.Log lines,
    ///     including the mod loader's "Activating(n)" and "Failed to activate mod", and BepInEx's lines, which BepInEx
    ///     mirrors there with a [Level:Source] prefix. BepInEx's LogOutput.log leaves Unity's lines out.
    ///     The file has no timestamps ("t": null) and no level for Unity lines, which are recorded as Info,
    ///     or Error when they name an exception. Indented lines continue the entry above (stack traces).
    /// </summary>
    private static void Backfill()
    {
        var path = UnityEngine.Application.consoleLogPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
            return;
        }

        string text;
        try {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(fs);
            text = reader.ReadToEnd();
        } catch (Exception ex) {
            RmcpMod.Log.LogWarning($"could not read {path} for the startup log: {ex.Message}");
            return;
        }

        Entry? current = null;
        foreach (var raw in text.Split('\n')) {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) {
                current = null;
                continue;
            }

            if (line.StartsWith("(Filename:")) {
                continue;
            }

            if (current is not null && char.IsWhiteSpace(line[0])) {
                current.Message += "\n" + line;
                continue;
            }

            var m = _prefixed.Match(line);
            current = m.Success
                ? new(null, m.Groups[1].Value, m.Groups[2].Value.Trim(), m.Groups[3].Value)
                : new(null, line.Contains("Exception") ? "Error" : "Info", "Unity Log", line);
            Add(current);
        }
    }

    /// <summary>Entries with seq &gt; after, at most the newest <paramref name="limit" />; next is the cursor for the following read.</summary>
    internal static JObject Read(long after, int limit)
    {
        Entry[] startup;
        Entry[] live;
        long next;
        long first;
        lock (_lock) {
            startup = _startup.Where(e => e.Seq > after).ToArray();
            live = _entries.Where(e => e.Seq > after).ToArray();
            next = _seq;
            first = _entries.Count > 0 ? _entries.Peek().Seq : _seq + 1;
        }

        // limit trims live entries only; the pinned startup lines always come along
        var hits = startup.Concat(live.Skip(Math.Max(0, live.Length - limit)));
        return new() {
            ["next"] = next,
            ["first"] = first,
            ["entries"] = new JArray(hits.Select(e => new JObject {
                ["seq"] = e.Seq,
                // invariant: under th-TH or ar-SA the current culture would write a Buddhist or Hijri year
                ["t"] = e.Time?.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
                ["level"] = e.Level,
                ["source"] = e.Source,
                ["msg"] = e.Message,
            })),
        };
    }

    private sealed class Entry(DateTime? time, string level, string source, string message)
    {
        public long Seq;
        public readonly DateTime? Time = time;
        public readonly string Level = level;
        public readonly string Source = source;
        public string Message = message;
    }
}
