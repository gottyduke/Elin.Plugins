using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.String;

namespace Cwl.Helper.FileUtil;

public class FileMapping
{
    private static readonly List<(string, string)> _fallbacks = [
        new("ZHTW", "CN"),
        new("CN", "ZHTW"),
        new("*", "EN"),
    ];

    private readonly List<string> _indexed = [];
    private readonly List<FileInfo> _sources = [];

    public FileMapping(ModPackage owner, string langCode = "EN")
    {
        Owner = owner;
        RebuildLangModMapping(langCode);
    }

    public bool Mounted => Primary?.Exists ?? false;
    public ModPackage Owner { get; init; }

    public DirectoryInfo? Primary { get; private set; }

    public IEnumerable<FileInfo> Sources => _sources;

    public DirectoryInfo ModBaseDir => Owner.dirInfo;


    public static ILookup<string, string> FallbackLut => field ??= _fallbacks.ToLookup(r => r.Item1, r => r.Item2);

    public void RebuildLangModMapping(string langCode = "EN")
    {
        Primary = null;
        _indexed.Clear();
        _sources.Clear();

        var baseDir = Owner.dirInfo.FullName;
        var langMod = Path.Combine(baseDir, "LangMod");
        if (!Directory.Exists(langMod)) {
            return;
        }

        var resources = Directory.GetDirectories(langMod);
        if (resources.Length != 0) {
            // use FallbackLut to get an ordered list of language codes to check
            var ordering = new HashSet<string>(StringComparer.Ordinal);
            ordering.UnionWith([langCode, ..FallbackLut[langCode], ..FallbackLut["*"]]);

            var resourceSet = resources.ToHashSet(PathTruncation.PathComparer);

            // 1. explicit ordered/fallback language folders that exist
            var providers = ordering
                .Select(order => Path.Combine(langMod, order))
                .Where(path => resourceSet.Contains(path));
            foreach (var path in providers) {
                _indexed.Add(path);
                resourceSet.Remove(path);
            }

            // 2. remaining resource folders (any other language)
            _indexed.AddRange(resourceSet);

            // 3. fallback mappings
            _indexed.Add(langMod);
        }

        _indexed.Add(baseDir);

        Primary = new(_indexed[0]);

        if (!Mounted) {
            return;
        }

        foreach (var index in _indexed.Take(_indexed.Count - 1)) {
            var sources = Directory.GetFiles(index, "*.xlsx", SearchOption.TopDirectoryOnly);
            if (sources.Length == 0) {
                continue;
            }

            _sources.AddRange(sources.Select(f => new FileInfo(f)));
            if (_sources.Count > 0) {
                break;
            }
        }
    }

    public FileInfo? RelocateFile(string relativePath)
    {
        if (relativePath.IsInvalidPath() || _indexed.Count == 0) {
            return null;
        }

        return _indexed
            .Select(mapping => new FileInfo(Path.Combine(mapping, relativePath)))
            .FirstOrDefault(file => file.Exists);
    }

    public DirectoryInfo? RelocateDir(string relativePath)
    {
        if (relativePath.IsInvalidPath() || _indexed.Count == 0) {
            return null;
        }

        return _indexed
            .Select(mapping => new DirectoryInfo(Path.Combine(mapping, relativePath)))
            .FirstOrDefault(dir => dir.Exists);
    }
}