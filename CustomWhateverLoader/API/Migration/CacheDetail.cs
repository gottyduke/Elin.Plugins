using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API.Processors;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl.API.Migration;

[ConsoleCommandClassCustomizer("cwl.data")]
public sealed class CacheDetail(string cacheKey)
{
    private static readonly GameIOProcessor.GameIOContext _context = GameIOProcessor.GetPersistentModContext(ModInfo.Name)!;

    private static readonly Dictionary<string, CacheDetail> _details = [];

    private readonly Dictionary<string, SourceData.BaseRow[]> _cache = [];
    private bool _dirty;

    public long BlobSize =>
        _context.GetChunkFile(cacheKey) is { Exists: true } file
            ? file.Length
            : 0L;

    public void Delete()
    {
        _cache.Clear();
        _context.Remove(cacheKey);

        var index = _details.FirstOrDefault(kv => kv.Value == this);
        if (index.Key is not null) {
            _details.Remove(index.Key);
        }
    }

    public bool TryGetCache(string key, out SourceData.BaseRow[] rows)
    {
        return _cache.TryGetValue(key, out rows);
    }

    public void EmplaceCache(string key, SourceData.BaseRow[] rows)
    {
        _cache[key] = rows;
        _dirty = true;
    }

    public void GenerateCache()
    {
        _context.Save(_cache, cacheKey);
    }

    public static CacheDetail GetOrAdd(FileInfo file)
    {
        var cacheKey = $"blob_{file.ShortPath()}_{file.LastWriteTimeUtc}".GetSha256Code();
        if (_details.TryGetValue(cacheKey, out var detail)) {
            return detail;
        }

        detail = _details[cacheKey] = new(cacheKey);

        if (!_context.Load(out Dictionary<string, SourceData.BaseRow[]>? cache, cacheKey) ||
            cache is null) {
            return detail;
        }

        foreach (var (k, v) in cache) {
            detail._cache[k] = v;
        }

        return detail;
    }

    public static void InvalidateCache()
    {
        if (CacheVersionManifest.Get()?.ValidateManifest() is not true) {
            ClearCache();
        }
    }

    [ConsoleCommand("clear_source_cache")]
    public static string ClearCache()
    {
        _context.Clear();
        _context.Save(new CacheVersionManifest(ModInfo.Version, DateTime.UtcNow), "cache_manifest");

        return "source sheets cache cleared";
    }

    [Time]
    public static void FinalizeCache(bool dirtyOnly = true)
    {
        var details = _details.Values.Where(c => !dirtyOnly || c._dirty).ToArray();
        foreach (var detail in details) {
            detail.GenerateCache();
            detail._dirty = false;
        }

        if (details.Length > 0) {
            CwlMod.Popup<CacheDetail>("cwl_ui_cache_gen".Loc(CacheVersionManifest.Get()?.NextGen(), GetDetailString(details)));
        }
    }

    public static void ClearDetail()
    {
        _details.Clear();
    }

    public static string GetDetailString(CacheDetail[] details)
    {
        return "cwl_log_cache_detail".Loc(details.Length, details.Sum(d => d.BlobSize).ToAllocateString());
    }

    internal sealed record CacheVersionManifest(string Version, DateTime Retention)
    {
        internal static CacheVersionManifest? Get()
        {
            _context.Load(out CacheVersionManifest? manifest, "cache_manifest");
            return manifest;
        }

        internal bool ValidateManifest()
        {
            if (Version != ModInfo.Version) {
                CwlMod.Log<CacheDetail>($"cache manifest version mismatch, read: {Version}, current: {ModInfo.Version}");
                return false;
            }

            var lifetime = (DateTime.UtcNow - Retention).Days;
            var retention = CwlConfig.CacheSourceSheetsRetention;
            if (lifetime >= retention) {
                CwlMod.Log<CacheDetail>($"cache manifest out of date, current: {lifetime}, retention: {retention}");
                return false;
            }

            CwlMod.Debug<CacheVersionManifest>($"next retention: {retention - lifetime} day(s)");
            return true;
        }

        internal int NextGen()
        {
            var lifetime = (DateTime.UtcNow - Retention).Days;
            var retention = CwlConfig.CacheSourceSheetsRetention;
            return retention - lifetime;
        }
    }
}