using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API.Processors;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using MethodTimer;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.API.Migration;

[ConsoleCommandClassCustomizer("cwl.data")]
public sealed class CacheDetail(string cacheKey)
{
    private const string CacheVersionV1 = "1.20.49";
    private const string CacheStorage = ModInfo.Name;

    private static readonly GameIOProcessor.GameIOContext _context = GameIOProcessor.GetPersistentModContext(CacheStorage)!;
    private static readonly ConcurrentDictionary<string, CacheDetail> _details = [];

    private SerializableSourceCache _cache = [];

    public long BlobSize => _context.GetChunkFile(cacheKey) is { Exists: true } file ? file.Length : 0L;

    public bool DirtyOrEmpty { get; private set; } = true;
    public FileInfo SheetFile { get; private init; } = null!;
    public IReadOnlyDictionary<string, SourceData.BaseRow[]> Source => _cache;
    public MigrateDetail MigrateDetail => MigrateDetail.GetOrAdd(SheetFile);

    /// <summary>
    ///     Deletes the cached data and removes the detail from the cache manager.
    /// </summary>
    public void Delete()
    {
        _cache.Clear();
        _context.Remove(cacheKey);

        var index = _details.FirstOrDefault(kv => kv.Value == this);
        if (index.Key is not null) {
            _details.Remove(index.Key, out _);
        }
    }

    /// <summary>
    ///     Tries to retrieve cached rows for a given key.
    /// </summary>
    /// <param name="key">The sheet name for the cached data.</param>
    /// <param name="rows">The output cached rows if found.</param>
    /// <returns>true if the data was found, otherwise false.</returns>
    public bool TryGetCache(string key, out SourceData.BaseRow[] rows)
    {
        return _cache.TryGetValue(key, out rows);
    }

    /// <summary>
    ///     Adds or updates cached rows for a given key and marks the cache as dirty.
    /// </summary>
    /// <param name="key">The sheet name for the cached data.</param>
    /// <param name="rows">The array of rows to cache.</param>
    public void EmplaceCache(string key, SourceData.BaseRow[] rows)
    {
        _cache[key] = rows;
        DirtyOrEmpty = true;
    }

    /// <summary>
    ///     Writes the cache to the file blob.
    /// </summary>
    public void GenerateCache()
    {
        _context.Save(_cache, cacheKey);
    }

    /// <summary>
    ///     Gets an existing CacheDetail for a file or creates a new one. <br />
    ///     The CacheDetail is unique to the file's last written time.
    /// </summary>
    /// <param name="file">The FileInfo object for the source file.</param>
    /// <returns>The CacheDetail instance for the specified file.</returns>
    public static CacheDetail GetOrAdd(FileInfo file)
    {
        var cacheKey = $"blob_{file.ShortPath()}_{file.LastWriteTimeUtc}".GetSha256Code();
        if (_details.TryGetValue(cacheKey, out var detail)) {
            return detail;
        }

        detail = _details[cacheKey] = new(cacheKey) {
            SheetFile = file,
        };

        if (!_context.Load<SerializableSourceCache>(out var cache, cacheKey)) {
            return detail;
        }

        detail._cache = cache;
        detail.DirtyOrEmpty = false;

        return detail;
    }

    /// <summary>
    ///     Invalidates the cache if the version manifest is outdated.
    ///     Clears the cache completely if the manifest is invalid.
    /// </summary>
    public static void InvalidateCache()
    {
        if (CacheVersionManifest.Get()?.ValidateManifest() is not true) {
            ClearCache();
        }
    }

    /// <summary>
    ///     Clears all cached source sheet data and creates a new cache manifest.
    /// </summary>
    /// <returns>Details of the cache has been cleared and the next generation time.</returns>
    [ConsoleCommand("clear_source_cache")]
    public static string ClearCache()
    {
        var manifest = new CacheVersionManifest(CacheVersionV1, DateTime.UtcNow);

        _context.Clear();
        _context.SaveUncompressed(manifest, "cache_manifest");

        return $"Source sheets cache cleared, next generation in {manifest.NextGen()} days";
    }

    /// <summary>
    ///     Finalizes and saves all dirty caches to file blob.
    /// </summary>
    /// <param name="dirtyOnly">If true, only saves caches that have been modified.</param>
    [Time]
    public static void FinalizeCache(bool dirtyOnly = true)
    {
        var details = _details.Values
            .Where(c => !dirtyOnly || c.DirtyOrEmpty)
            .ToArray();
        using var sb = StringBuilderPool.Get();

        foreach (var detail in details) {
            detail.GenerateCache();
            detail.DirtyOrEmpty = false;

            sb.AppendLine(detail.MigrateDetail.Mod?.title);
        }

        if (details.Length > 0) {
            var list = sb.ToString();
            using var progress = ProgressIndicator.CreateProgressScoped(
                () => new("cwl_ui_cache_gen".Loc(CacheVersionManifest.Get()?.NextGen(), GetDetailString(details))),
                5f);
            progress.Get<ProgressIndicator>().OnHover(p => GUILayout.Label(list, p.GUIStyle));
        }
    }

    /// <summary>
    ///     Clears the in-memory dictionary of CacheDetail instances.
    /// </summary>
    public static void ClearDetail()
    {
        _details.Clear();
    }

    /// <summary>
    ///     Generates a localized string detailing the number of caches and their total size.
    /// </summary>
    /// <param name="details">An array of CacheDetail objects.</param>
    /// <returns>A formatted string with cache details.</returns>
    public static string GetDetailString(CacheDetail[] details)
    {
        return "cwl_log_cache_detail".Loc(details.Length, details.Sum(d => d.BlobSize).ToAllocateString());
    }

    internal sealed record CacheVersionManifest(string Version, DateTime Retention)
    {
        /// <summary>
        ///     Retrieves the cache manifest from storage.
        /// </summary>
        /// <returns>The CacheVersionManifest instance, or null if it doesn't exist.</returns>
        internal static CacheVersionManifest? Get()
        {
            _context.Load(out CacheVersionManifest? manifest, "cache_manifest");
            return manifest;
        }

        /// <summary>
        ///     Validates the manifest's version and retention period.
        /// </summary>
        /// <returns>True if the manifest is valid, otherwise false.</returns>
        internal bool ValidateManifest()
        {
            if (Version != CacheVersionV1) {
                CwlMod.Log<CacheDetail>($"cache manifest version mismatch, read: {Version}, current: {CacheVersionV1}");
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

        /// <summary>
        ///     Calculates the number of days until the cache retention period expires.
        /// </summary>
        /// <returns>The number of days remaining until the cache needs to be regenerated.</returns>
        internal int NextGen()
        {
            var lifetime = (DateTime.UtcNow - Retention).Days;
            var retention = CwlConfig.CacheSourceSheetsRetention;
            return retention - lifetime;
        }
    }
}