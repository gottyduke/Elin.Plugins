using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.Helper.FileUtil;

[ConsoleCommandClassCustomizer("cwl.data")]
public class PackageIterator
{
    private static readonly Dictionary<string, string> _cachedPaths = [];
    private static readonly Dictionary<string, FileMapping> _mappings = [];

    public static IEnumerable<string> GetLangFilesFromPackage(string pattern, bool excludeBuiltIn = false)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated)
            .Where(p => !excludeBuiltIn || !p.builtin)
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Lang*"))
            .SelectMany(d => d.GetFiles(pattern, SearchOption.AllDirectories))
            .Select(f => f.FullName)
            .Select(PathNormalizer.NormalizePath);
    }

    public static IEnumerable<DirectoryInfo> GetLangModFilesFromPackage(string? modId = null)
    {
        return GetLoadedPackagesAsMapping(modId)
            .Select(m => m.Primary)
            .OfType<DirectoryInfo>();
    }

    public static IEnumerable<FileInfo> GetSourcesFromPackage(string? modId = null)
    {
        return GetLoadedPackagesAsMapping(modId)
            .SelectMany(m => m.Sources);
    }

    public static IEnumerable<DirectoryInfo> GetSoundFilesFromPackage(string? modId = null)
    {
        return GetLoadedPackages(modId)
            .SelectMany(d => d.GetDirectories("Sound"));
    }

    public static IEnumerable<DirectoryInfo> GetLoadedPackages(string? modId = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .Select(p => p.dirInfo);
    }

    public static IEnumerable<FileMapping> GetLoadedPackagesAsMapping(string? modId = null)
    {
        if (modId is not null && _mappings.TryGetValue(modId, out var mapping)) {
            return [mapping];
        }

        return BaseModManager.Instance.packages
            .OfType<ModPackage>()
            .Where(p => p.activated && !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .Select(GetPackageMapping);
    }

    public static FileMapping GetPackageMapping(ModPackage package)
    {
        if (_mappings.TryGetValue(package.id, out var mapping)) {
            return mapping;
        }

        var lang = Core.Instance.config?.lang ?? "EN";
        return _mappings[package.id] = new(package, lang);
    }

    public static IEnumerable<ExcelData> GetExcelsFromPackage(string relativePath, int startIndex = 5)
    {
        return GetRelocatedFilesFromPackage(relativePath)
            .Select(b => new ExcelData(b.FullName, startIndex));
    }

    public static IEnumerable<(FileInfo, T)> GetJsonsFromPackage<T>(string relativePath) where T : new()
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => GetJsonFromPackage<T>(relativePath, p.id))
            .Where(jd => jd.Item1 is not null && jd.Item2 is not null)
            .OfType<(FileInfo, T)>();
    }

    public static IEnumerable<FileInfo> GetRelocatedFilesFromPackage(string relativePath)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => GetRelocatedFileFromPackage(relativePath, p.id))
            .OfType<FileInfo>();
    }

    public static ExcelData? GetExcelFromPackage(string relativePath, string modId, int startIndex = 5)
    {
        var excel = GetRelocatedFileFromPackage(relativePath, modId);
        return excel is null ? null : new(excel.FullName, startIndex);
    }

    public static (FileInfo?, T?) GetJsonFromPackage<T>(string relativePath, string modId) where T : new()
    {
        var json = GetRelocatedFileFromPackage(relativePath, modId);
        ConfigCereal.ReadConfig<T>(json?.FullName, out var data);
        return (json, data);
    }

    public static FileInfo? GetRelocatedFileFromPackage(string relativePath, string modId)
    {
        var resources = GetLoadedPackagesAsMapping(modId).LastOrDefault();
        return resources?.RelocateFile(relativePath);
    }

    public static bool TryLoadFromPackageCache(string cacheName, out string path)
    {
        path = "";
        return CwlConfig.CachePaths && _cachedPaths.TryGetValue(cacheName, out path) && File.Exists(path);
    }

    public static void AddCachedPath(string cacheName, string path)
    {
        _cachedPaths[cacheName] = path;
    }

    [ConsoleCommand("clear_path_cache")]
    [CwlLangReload]
    internal static void ClearCache()
    {
        foreach (var mapping in _mappings.Values) {
            mapping.RebuildLangModMapping(Core.Instance.config?.lang ?? "EN");
        }

        CwlMod.Log<PackageIterator>("cleared paths cache");
    }
}