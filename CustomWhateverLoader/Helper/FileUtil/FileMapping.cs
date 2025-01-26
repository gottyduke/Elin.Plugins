using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cwl.Helper.FileUtil;

public class FileMapping
{
    private static readonly List<FallbackRule> _fallbacks = [
        new("ZHTW", "CN"),
        new("*", "EN"),
    ];

    private static ILookup<string, string>? _fallbackLut;
    private readonly List<string> _indexed = [];

    public FileMapping(ModPackage owner, string langCode = "EN")
    {
        Owner = owner;
        RebuildLangModMapping(langCode);
    }

    public bool Mounted => Primary?.Exists ?? false;
    public ModPackage Owner { get; init; }
    public DirectoryInfo? Primary { get; private set; }

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

        var resources = Directory.GetDirectories(langMod).ToArray();
        if (resources.Length == 0) {
            return;
        }

        SortedSet<string> ordering = [langCode, ..FallbackLut[langCode], ..FallbackLut["*"]];
        SortedSet<string> indexed = [
            ..ordering.Select(order => Path.Combine(langMod, order)).Where(resources.Contains),
            ..resources,
        ];

        _indexed.AddRange(indexed);
        if (_indexed.Count > 0) {
            Primary = new(_indexed[0]);
        }
    }

    public FileInfo? RelocateFile(string relativePath)
    {
        return _indexed.Select(mapping => new FileInfo(Path.Combine(mapping, relativePath))).FirstOrDefault(file => file.Exists);
    }

    private record FallbackRule(string LangCode, string Fallback);
}