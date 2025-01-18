using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API;
using Cwl.Helper.String;

namespace Cwl.Helper.FileUtil;

public static class PackageIterator
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
            .Select(PathExt.NormalizePath);
    }

    public static IEnumerable<DirectoryInfo> GetLangModFilesFromPackage(string? modGuid = null)
    {
        var lang = Core.Instance.config?.lang ?? "EN";
        return GetLoadedPackages(modGuid)
            .SelectMany(d => d.GetDirectories("LangMod"))
            .Select(d => {
                var dirs = d.GetDirectories();
                return dirs.FirstOrDefault(sd => sd.Name == lang) ??
                       // 1.17 use CN as ZHTW fallback
                       dirs.FirstOrDefault(sd => lang == "ZHTW" && sd.Name == "CN") ??
                       // 1.7 use EN as 1st fallback
                       dirs.FirstOrDefault(sd => sd.Name == "EN") ??
                       dirs.FirstOrDefault();
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

    public static IEnumerable<ExcelData> GetRelocatedExcelsFromPackage(string relativePath, int startIndex = 5)
    {
        return GetRelocatedFilesFromPackage(relativePath)
            .Select(b => new ExcelData(b.FullName, startIndex));
    }

    public static IEnumerable<FileInfo> GetRelocatedFilesFromPackage(string relativePath)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => GetRelocatedFileFromPackage(relativePath, p.id))
            .OfType<FileInfo>();
    }

    public static ExcelData? GetRelocatedExcelFromPackage(string relativePath, string modGuid, int startIndex = 5)
    {
        var excel = GetRelocatedFileFromPackage(relativePath, modGuid);
        return excel is null ? null : new(excel.FullName, startIndex);
    }

    public static FileInfo? GetRelocatedFileFromPackage(string relativePath, string modGuid)
    {
        var cacheName = $"{modGuid}/Resources";
        if (!TryLoadFromPackageCache(cacheName, out var cachedPath)) {
            var resources = GetLangModFilesFromPackage(modGuid).FirstOrDefault();
            if (resources?.Exists is not true) {
                return null;
            }

            cachedPath = resources.FullName;
            AddCachedPath(cacheName, cachedPath);
        }

        var file = Path.Combine(cachedPath, relativePath);
        return File.Exists(file) ? new(file) : null;
    }

    public static bool TryLoadFromPackageCache(string cacheName, out string path)
    {
        path = string.Empty;
        return CwlConfig.CachePaths && _cachedPaths.TryGetValue(cacheName, out path);
    }

    public static void AddCachedPath(string cacheName, string path)
    {
        _cachedPaths[cacheName] = path;
    }

    [CwlLangReload]
    internal static void ClearCache()
    {
        _cachedPaths.Clear();
    }
}