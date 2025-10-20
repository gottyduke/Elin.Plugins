using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.Helper.FileUtil;

/// <summary>
///     Utility for relocating file resources at runtime from packages
/// </summary>
[ConsoleCommandClassCustomizer("cwl.data")]
public class PackageIterator
{
    private static readonly Dictionary<string, string> _cachedPaths = [];
    private static readonly Dictionary<string, FileMapping> _mappings = [];
    private static readonly HashSet<string> _tempAdditionalLookup = new(StringComparer.Ordinal);

#region LangMod

    public static IEnumerable<string> GetLangFilesFromPackage(string pattern, bool excludeBuiltIn = false)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated)
            .Where(p => !excludeBuiltIn || !p.builtin)
            .Select(p => p.dirInfo)
            .SelectMany(d => d.GetDirectories("Lang*"))
            .SelectMany(d => d.GetFiles(pattern, SearchOption.AllDirectories))
            .Select(f => f.FullName)
            .Select(PathTruncation.NormalizePath);
    }

    public static IEnumerable<DirectoryInfo> GetLangModFilesFromPackage(string? modId = null)
    {
        return GetLoadedPackagesAsMapping(modId)
            .Select(m => m.Primary)
            .OfType<DirectoryInfo>();
    }

    public static IEnumerable<(DirectoryInfo, ModPackage)> GetLangModFilesFromPackageEx(string? modId = null)
    {
        return GetLoadedPackagesAsMapping(modId)
            .Select(m => (m.Primary, m.Owner))
            .Where(ex => ex.Primary is not null)!;
    }

#endregion

#region Sources

    public static IEnumerable<FileInfo> GetSourcesFromPackage(string? modId = null)
    {
        return GetLoadedPackagesAsMapping(modId)
            .SelectMany(m => m.Sources);
    }

    public static IEnumerable<(FileInfo, ModPackage)> GetSourcesFromPackageEx(string? modId = null)
    {
        return GetLoadedPackagesAsMapping(modId)
            .SelectMany(m => m.Sources, (m, s) => (s, m.Owner));
    }

#endregion

#region Sounds

    public static IEnumerable<DirectoryInfo> GetSoundFilesFromPackage(string? modId = null)
    {
        return GetLoadedPackages(modId)
            .SelectMany(d => d.GetDirectories("Sound"));
    }

    public static IEnumerable<(DirectoryInfo, ModPackage)> GetSoundFilesFromPackageEx(string? modId = null)
    {
        return GetLoadedPackagesEx(modId)
            .Select(m => (m.dirInfo.GetDirectories("Sound").FirstOrDefault(), m))
            .Where(ex => ex.Item1 is not null);
    }

#endregion

#region ExcelData

    public static IEnumerable<ExcelData> GetExcelsFromPackage(string relativePath, int startIndex = 5)
    {
        return GetRelocatedFilesFromPackage(relativePath)
            .Select(b => new ExcelData(b.FullName, startIndex));
    }

    public static ExcelData? GetExcelFromPackage(string relativePath, string modId, int startIndex = 5)
    {
        var excel = GetRelocatedFileFromPackage(relativePath, modId);
        return excel is null ? null : new(excel.FullName, startIndex);
    }

#endregion

#region Json

    public static IEnumerable<(FileInfo, T)> GetJsonsFromPackage<T>(string relativePath) where T : new()
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => GetJsonFromPackage<T>(relativePath, p.id))
            .Where(jd => jd.Item1 is not null && jd.Item2 is not null)
            .OfType<(FileInfo, T)>();
    }

    public static (FileInfo?, T?) GetJsonFromPackage<T>(string relativePath, string modId) where T : new()
    {
        var json = GetRelocatedFileFromPackage(relativePath, modId);
        ConfigCereal.ReadConfig<T>(json?.FullName, out var data);
        return (json, data);
    }

#endregion

#region Relocation File

    public static FileInfo? GetRelocatedFileFromPackage(string relativePath, string modId)
    {
        var resources = GetPackageMapping(modId);
        return resources?.RelocateFile(relativePath);
    }

    public static IEnumerable<FileInfo> GetRelocatedFilesFromPackage(string relativePath)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => GetRelocatedFileFromPackage(relativePath, p.id))
            .Concat(GetAdditionalLookup(relativePath))
            .OfType<FileInfo>();
    }

    public static IEnumerable<(FileInfo, ModPackage?)> GetRelocatedFilesFromPackageEx(string relativePath)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => (GetRelocatedFileFromPackage(relativePath, p.id), p as ModPackage))
            .Concat(GetAdditionalLookupEx(relativePath))
            .Where(fm => fm.Item1 is not null);
    }

#endregion

#region Relocation Dir

    public static DirectoryInfo? GetRelocatedDirFromPackage(string relativePath, string modId)
    {
        var resources = GetPackageMapping(modId);
        return resources?.RelocateDir(relativePath);
    }

    public static IEnumerable<DirectoryInfo> GetRelocatedDirsFromPackage(string relativePath)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => GetRelocatedDirFromPackage(relativePath, p.id))
            .Concat(GetAdditionalDirLookup(relativePath))
            .OfType<DirectoryInfo>();
    }

    public static IEnumerable<(DirectoryInfo, ModPackage?)> GetRelocatedDirsFromPackageEx(string relativePath)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => (GetRelocatedDirFromPackage(relativePath, p.id), p as ModPackage))
            .Concat(GetAdditionalDirLookupEx(relativePath))
            .Where(fm => fm.Item1 is not null);
    }

#endregion

#region Package

    public static IEnumerable<DirectoryInfo> GetLoadedPackages(string? modId = null)
    {
        return GetLoadedPackagesEx(modId)
            .Select(p => p.dirInfo);
    }

    public static IEnumerable<ModPackage> GetLoadedPackagesEx(string? modId = null)
    {
        return BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Where(p => modId is null || p.id == modId)
            .OfType<ModPackage>();
    }

#endregion

#region FileMapping

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

    public static FileMapping? GetPackageMapping(string modId)
    {
        return GetLoadedPackagesAsMapping(modId).FirstOrDefault();
    }

    public static FileMapping GetPackageMapping(ModPackage package)
    {
        if (_mappings.TryGetValue(package.id, out var mapping)) {
            return mapping;
        }

        var lang = Core.Instance.config?.lang ?? "EN";
        return _mappings[package.id] = new(package, lang);
    }

#endregion

#region Cache

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

#endregion

#region TempLookup

    public static ScopeExit AddTempLookupPaths(params string[] paths)
    {
        var scope = new ScopeExit {
            OnExit = ClearTempLookupPaths,
        };

        _tempAdditionalLookup.Clear();
        _tempAdditionalLookup.UnionWith(paths.Select(PathTruncation.NormalizePath));

        return scope;
    }

    /// <summary>
    ///     Add temporary paths to the end of package list
    /// </summary>
    /// <remarks>Must materialize the lookup after the temp lookup</remarks>
    public static ScopeExit AddTempLookupPathsEx(string modId, params string[] paths)
    {
        _mappings["cwl_temp_lookup"] = GetPackageMapping(modId)!;
        return AddTempLookupPaths(paths);
    }

    public static void ClearTempLookupPaths()
    {
        _mappings.Remove("cwl_temp_lookup");
        _tempAdditionalLookup.Clear();
    }

    internal static IEnumerable<FileInfo> GetAdditionalLookup(string relativePath)
    {
        return _tempAdditionalLookup
            .Select(p => new FileInfo(Path.Combine(p, relativePath)))
            .Where(f => f.Exists);
    }

    internal static IEnumerable<(FileInfo, ModPackage?)> GetAdditionalLookupEx(string relativePath)
    {
        var tempPackage = _mappings.GetValueOrDefault("cwl_temp_lookup")?.Owner;
        return _tempAdditionalLookup
            .Select(p => new FileInfo(Path.Combine(p, relativePath)))
            .Where(f => f.Exists)
            .Select(f => (f, tempPackage));
    }

    internal static IEnumerable<DirectoryInfo> GetAdditionalDirLookup(string relativePath)
    {
        return _tempAdditionalLookup
            .Select(p => new DirectoryInfo(Path.Combine(p, relativePath)))
            .Where(d => d.Exists);
    }

    internal static IEnumerable<(DirectoryInfo, ModPackage?)> GetAdditionalDirLookupEx(string relativePath)
    {
        var tempPackage = _mappings.GetValueOrDefault("cwl_temp_lookup")?.Owner;
        return _tempAdditionalLookup
            .Select(p => new DirectoryInfo(Path.Combine(p, relativePath)))
            .Where(d => d.Exists)
            .Select(d => (d, tempPackage));
    }

#endregion
}