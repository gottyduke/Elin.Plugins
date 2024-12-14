using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.String;

namespace Cwl.Helper.File;

public static class PackageFileIterator
{
    private static readonly Dictionary<string, string> _cachedPaths = [];

    public static IEnumerable<string> GetLangFilesFromPackage(string pattern, bool excludeBuiltIn = false)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated)
            .Where(p => !excludeBuiltIn || (excludeBuiltIn && !p.builtin))
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Lang*"))
            .SelectMany(d => d.GetFiles(pattern, SearchOption.AllDirectories))
            .Select(f => f.FullName)
            .Select(PathNormalizer.NormalizePath);
    }

    public static IEnumerable<DirectoryInfo> GetLangModFilesFromPackage(string? modGuid = null)
    {
        return GetLoadedPackages(modGuid)
            .SelectMany(d => d.GetDirectories("LangMod"))
            .Select(d => {
                var dirs = d.GetDirectories();
                return dirs.FirstOrDefault(sd => sd.Name == Core.Instance.config.lang) ??
                       // 1.7 use EN as 1st fallback
                       dirs.FirstOrDefault(sd => sd.Name == "EN") ??
                       dirs.First();
            });
    }

    public static IEnumerable<DirectoryInfo> GetSoundFilesFromPackage(string? modGuid = null)
    {
        return GetLoadedPackages(modGuid)
            .SelectMany(d => d.GetDirectories("Sound"));
    }

    public static IEnumerable<DirectoryInfo> GetLoadedPackages(string? modGuid = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Where(p => modGuid is null || p.id == modGuid)
            .Select(p => p.dirInfo);
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