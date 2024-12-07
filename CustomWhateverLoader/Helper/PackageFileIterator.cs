using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cwl.Helper;

public static class PackageFileIterator
{
    private static readonly Dictionary<string, string> _cachedPaths = [];

    public static IEnumerable<string> GetLangFilesFromPackage(string pattern, bool excludeBuiltIn = false)
    {
        return BaseModManager.Instance.packages
            .Where(p => !excludeBuiltIn || (excludeBuiltIn && !p.builtin))
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Lang*"))
            .SelectMany(d => d.GetFiles(pattern, SearchOption.AllDirectories))
            .Select(f => f.FullName)
            .Select(PathNormalizer.NormalizePath);
    }

    public static IEnumerable<DirectoryInfo> GetLangModFilesFromPackage(string? modId = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("LangMod"))
            .Select(d => d.GetDirectories().FirstOrDefault(sd => sd.Name == Lang.langCode)
                         ?? d.GetDirectories().First());
    }

    public static IEnumerable<DirectoryInfo> GetSoundFilesFromPackage(string? modId = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Sound"));
    }

    public static bool TryLoadFromPackage(string cacheName, out string path)
    {
        return _cachedPaths.TryGetValue(cacheName, out path);
    }

    public static void AddCachedPath(string cacheName, string path)
    {
        _cachedPaths[cacheName] = path;
    }
}