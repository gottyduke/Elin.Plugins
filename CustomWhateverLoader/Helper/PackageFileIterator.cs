using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cwl.Helper;

internal static class PackageFileIterator
{
    private static readonly Dictionary<string, string> _cachedPaths = [];

    internal static IEnumerable<string> GetLangFilesFromPackage(string pattern, bool excludeBuiltIn = false)
    {
        return BaseModManager.Instance.packages
            .Where(p => !excludeBuiltIn || (excludeBuiltIn && !p.builtin))
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Lang*"))
            .SelectMany(d => d.GetFiles(pattern, SearchOption.AllDirectories))
            .Select(f => f.FullName)
            .Select(PathNormalizer.NormalizePath);
    }

    internal static IEnumerable<DirectoryInfo> GetLandModFilesFromPackage(string? modId = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("LangMod"))
            .Select(d => d.GetDirectories().FirstOrDefault(sd => sd.Name == Lang.langCode)
                         ?? d.GetDirectories().First());
    }

    internal static IEnumerable<DirectoryInfo> GetSoundFilesFromPackage(string? modId = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Sound"));
    }

    internal static bool TryLoadFromPackage(string cacheName, out string path)
    {
        return _cachedPaths.TryGetValue(cacheName, out path);
    }

    internal static void AddCachedPath(string cacheName, string path)
    {
        _cachedPaths[cacheName] = path;
    }
}