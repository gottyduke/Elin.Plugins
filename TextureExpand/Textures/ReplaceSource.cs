using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TextureExpand.Textures;

internal sealed class ReplaceGroup
{
    internal ReplaceGroup(string? charaId, int tileId)
    {
        CharaId = charaId;
        TileId = tileId;
    }

    internal string? CharaId { get; }

    internal int TileId { get; }

    internal List<(string Key, string Path)> Entries { get; } = [];

    internal void Add(string key, string path)
    {
        if (!File.Exists(path)) {
            TextureExpand.LogError("Not found: " + path);
            return;
        }

        var index = Entries.FindIndex(e => e.Key == key);
        if (index >= 0) {
            Entries[index] = (key, path);
        } else {
            Entries.Add((key, path));
        }
    }
}

internal static class ReplaceSource
{
    private static readonly Regex _tilePattern = new(@"^objC_(\d+)", RegexOptions.Compiled);
    private static readonly Regex _charaPattern = new(@"^objC_([^#]+)", RegexOptions.Compiled);

    internal static List<ReplaceGroup> Collect()
    {
        var groups = new Groups();

        var files = SpriteReplacer.dictModItems
            .Where(kv => kv.Key.StartsWith("objC_"))
            .Select(kv => kv.Value + ".png")
            .ToList();

        for (var i = files.Count - 1; i >= 0; --i) {
            var key = Path.GetFileNameWithoutExtension(files[i]);
            FindGroup(groups, key)?.Add(key, files[i]);
        }

        foreach (var dir in BaseModManager.listChainLoad) {
            foreach (var file in Directory.GetFiles(dir, "*.textureexpand", SearchOption.TopDirectoryOnly)) {
                TextureExpand.Log("Parse setting: " + file);
                ParseConfig(groups, file);
            }
        }

        return groups.All;
    }

    private static void ParseConfig(Groups groups, string file)
    {
        foreach (var line in File.ReadLines(file)) {
            if (line.IsEmpty() || line.StartsWith("#")) {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2) {
                TextureExpand.LogError("Invalid line: please specify path and key with ',': " + line);
                continue;
            }

            var key = parts[1].Trim();
            var group = FindGroup(groups, key);
            if (group is null) {
                TextureExpand.LogError("Invalid key: key must start with objC_{tileId} or objC_{charaId}: " + key);
                continue;
            }

            var path = parts[0].Trim();
            if (!Path.IsPathRooted(path)) {
                path = Path.Combine(Path.GetDirectoryName(file)!, path);
            }

            group.Add(key, path);
        }
    }

    private static ReplaceGroup? FindGroup(Groups groups, string key)
    {
        var match = _tilePattern.Match(key);
        if (match.Success) {
            var tileId = int.Parse(match.Groups[1].Value);
            if (!groups.ByTile.TryGetValue(tileId, out var group)) {
                groups.ByTile[tileId] = group = new(null, tileId);
                groups.All.Add(group);
            }

            return group;
        }

        match = _charaPattern.Match(key);
        if (!match.Success) {
            return null;
        }

        var charaId = match.Groups[1].Value;
        if (!groups.ByChara.TryGetValue(charaId, out var charaGroup)) {
            groups.ByChara[charaId] = charaGroup = new(charaId, -1);
            groups.All.Add(charaGroup);
        }

        return charaGroup;
    }

    private sealed class Groups
    {
        internal readonly List<ReplaceGroup> All = [];
        internal readonly Dictionary<string, ReplaceGroup> ByChara = [];
        internal readonly Dictionary<int, ReplaceGroup> ByTile = [];
    }
}