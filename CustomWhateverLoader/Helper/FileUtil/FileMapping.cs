using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cwl.Helper.FileUtil;

public class FileMapping
{
    private static readonly List<FallbackRule> _fallbacks = [
        new("ZHTW", "CN"),
        new("CN", "ZHTW"),
        new("*", "EN"),
    ];

    private static ILookup<string, string>? _fallbackLut;
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

    public static ILookup<string, string> FallbackLut => _fallbackLut ??= _fallbacks.ToLookup(r => r.LangCode, r => r.Fallback);

    public void RebuildLangModMapping(string langCode = "EN")
    {
        Primary = null;
        _indexed.Clear();

        var langMod = Path.Combine(Owner.dirInfo.FullName, "LangMod");
        if (!Directory.Exists(langMod)) {
            return;
        }

        var resources = Directory.GetDirectories(langMod);
        if (resources.Length == 0) {
            return;
        }

        HashSet<string> ordering = [langCode, ..FallbackLut[langCode], ..FallbackLut["*"]];
        HashSet<string> indexed = [
            ..ordering.Select(order => Path.Combine(langMod, order)).Where(resources.Contains),
            ..resources,
        ];

        _indexed.AddRange(indexed);
        if (_indexed.Count > 0) {
            Primary = new(_indexed[0]);
        }

        if (!Mounted) {
            return;
        }

        foreach (var index in _indexed) {
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
        return _indexed.Select(mapping => new FileInfo(Path.Combine(mapping, relativePath))).FirstOrDefault(file => file.Exists);
    }

    private record FallbackRule(string LangCode, string Fallback);
}