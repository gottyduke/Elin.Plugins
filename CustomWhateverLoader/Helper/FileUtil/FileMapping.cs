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

    public FileMapping(ModPackage owner, string langCode = "EN")
    {
        Owner = owner;
        Sources = [];
        RebuildLangModMapping(langCode);
    }

    public bool Mounted => Primary?.Exists ?? false;
    public ModPackage Owner { get; init; }

    public DirectoryInfo? Primary { get; private set; }

    // primary dir with source sheets
    public IEnumerable<FileInfo> Sources { get; private set; }

    public DirectoryInfo ModBaseDir => Owner.dirInfo;

    public static ILookup<string, string>? FallbackLut => field ??= _fallbacks.ToLookup(r => r.Item1, r => r.Item2);

    public void RebuildLangModMapping(string langCode = "EN")
    {
        Primary = null;
        _indexed.Clear();

        var baseDir = Owner.dirInfo.FullName;
        var langMod = Path.Combine(baseDir, "LangMod");
        if (!Directory.Exists(langMod)) {
            return;
        }

        var resources = Directory.GetDirectories(langMod);
        if (resources.Length == 0) {
            return;
        }

        HashSet<string> ordering = [langCode, ..FallbackLut![langCode], ..FallbackLut["*"]];
        HashSet<string> indexed = [
            ..ordering
                .Select(order => Path.Combine(langMod, order))
                .Where(resources.Contains),
            ..resources,
            // fallback mappings
            langMod,
            baseDir,
        ];

        _indexed.AddRange(indexed);
        if (_indexed.Count > 0) {
            Primary = new(_indexed[0]);
        }

        if (!Mounted) {
            return;
        }

        // do not include baseDir in source mappings
        foreach (var index in _indexed.ToArray()[..^1]) {
            var sources = Directory.GetFiles(index, "*.xlsx", SearchOption.TopDirectoryOnly);
            if (sources.Length == 0) {
                continue;
            }

            Sources = sources.Select(f => new FileInfo(f));
            break;
        }
    }

    public FileInfo? RelocateFile(string relativePath)
    {
        if (relativePath.IsInvalidPath()) {
            return null;
        }

        return _indexed
            .Select(mapping => new FileInfo(Path.Combine(mapping, relativePath)))
            .FirstOrDefault(file => file.Exists);
    }
}