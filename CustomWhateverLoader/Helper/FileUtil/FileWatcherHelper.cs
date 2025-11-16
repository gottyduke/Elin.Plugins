using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Cwl.LangMod;

namespace Cwl.Helper.FileUtil;

public class FileWatcherHelper
{
    private static readonly Dictionary<string, (FileSystemWatcher, Action<FileSystemEventArgs>)> _watchers = [];

    private static bool SupportsFileSystemWatcher()
    {
        var v = Environment.OSVersion.Version;
        // Windows 8 (6.2) or higher
        return v.Major > 6 || v is { Major: 6, Minor: >= 2 };
    }

    public static void Register(string id,
                                string path,
                                string filter,
                                Action<FileSystemEventArgs> handler,
                                [CallerMemberName] string caller = "")
    {
        if (_watchers.ContainsKey(id)) {
            return;
        }

        if (!SupportsFileSystemWatcher()) {
            CwlMod.Warn<FileWatcherHelper>($"disabled for unsupported OS (Windows 7). ID={id}");
            return;
        }

        var watcher = new FileSystemWatcher(path, filter) {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                           | NotifyFilters.LastWrite
                           | NotifyFilters.CreationTime
                           | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        watcher.Created += SafeNotify;
        watcher.Deleted += SafeNotify;
        watcher.Changed += SafeNotify;
        watcher.Renamed += SafeNotify;

        _watchers[id] = (watcher, handler);

        CwlMod.Log<FileWatcherHelper>("cwl_log_processor_add".Loc("file_watch", $"{id}:{filter}", caller));

        return;

        void SafeNotify(object _, FileSystemEventArgs args)
        {
            try {
                handler(args);
            } catch (Exception ex) {
                CwlMod.Warn<FileWatcherHelper>("cwl_warn_processor".Loc("file_watch", $"{id}:{filter}", ex));
            }
        }
    }

    public static void Unregister(string id)
    {
        if (!_watchers.Remove(id, out var watcher)) {
            return;
        }

        watcher.Item1.EnableRaisingEvents = false;
        watcher.Item1.Dispose();
    }
}