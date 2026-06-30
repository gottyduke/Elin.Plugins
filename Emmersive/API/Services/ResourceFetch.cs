using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Emmersive.API.Services;

public class ResourceFetch
{
    private const string DefaultResource = "Emmersive.package.LangMod.";

    // holds user custom edits
    private static readonly Dictionary<ResourceKey, string> _activeResources = [];

    // holds active data exchanges
    public static readonly GameIOContext Context = GameIOContext.GetPersistentModContext("Emmersive")!;

    public static ResourceKey CustomFolder { get; private set; } =
        new(Path.Combine(Application.persistentDataPath, "Emmersive/Custom"));

    public static IEnumerable<ResourceDescriptor> GetAvailableResources(ResourceKey key)
    {
        using var _ = PackageIterator.AddTempLookup(CustomFolder);
        return PackageIterator
            .GetFilesEx(key)
            .Select(fm => new ResourceDescriptor(fm.file, fm.package?.title ?? "Custom"))
            .ToArray();
    }

    public static string GetDefaultResource(string manifest)
    {
        manifest = manifest.SanitizeFileName('.');
        var manifestPath = $"{DefaultResource}.{Lang.langCode}.{manifest}";
        var ms = EmMod.Assembly.GetManifestResourceStream(manifestPath);

        try {
            if (ms is null) {
                var fallbackManifest = $"{DefaultResource}.EN.{manifest}";
                ms = EmMod.Assembly.GetManifestResourceStream(fallbackManifest);
            }

            if (ms is null) {
                return "";
            }

            using var sr = new StreamReader(ms);
            return sr.ReadToEnd();
        } finally {
            ms?.Dispose();
        }
    }

    public sealed record ResourceDescriptor(FileInfo Provider, string PackageName = "");

#region Active Resource

    public static bool HasActiveResource(ResourceKey key)
    {
        return _activeResources.ContainsKey(key);
    }

    public static string GetActiveResource(ResourceKey key, bool autoSet = true)
    {
        if (_activeResources.TryGetValue(key, out var resource)) {
            return resource;
        }

        if (!autoSet) {
            return "";
        }

        var fs = GetAvailableResources(key);
        var fallbackResource = fs.LastOrDefault();
        resource = fallbackResource is not null
            ? File.ReadAllText(fallbackResource.Provider.FullName)
            : "";

        SetActiveResource(key, resource);

        return resource;
    }

    public static void SetActiveResource(ResourceKey key, string content)
    {
        _activeResources[key] = content;
        EmMod.Log<ResourceFetch>($"set active resource {key}");
    }

    public static void RemoveActiveResource(ResourceKey key)
    {
        _activeResources.Remove(key);
        EmMod.Log<ResourceFetch>($"removed active resource {key}");
    }

    public static void ClearActiveResources()
    {
        _activeResources.Clear();
        EmMod.Popup<ResourceFetch>("em_ui_clear_active".lang());
    }

#endregion

#region Custom Resource

    public static void SetCustomFolderPath(ResourceKey key)
    {
        CustomFolder = key;
        Directory.CreateDirectory(CustomFolder);
    }

    public static string? GetCustomResource(ResourceKey key)
    {
        var file = CustomFolder + key;
        return !File.Exists(file) ? null : File.ReadAllText(file);
    }

    public static void SetCustomResource(ResourceKey key, string content)
    {
        var file = CustomFolder + key;

        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);

        RemoveActiveResource(key);

        EmMod.Log<ResourceFetch>($"set custom resource {file}");
    }

    public static void RemoveCustomResource(ResourceKey key)
    {
        var file = CustomFolder + key;

        try {
            File.Delete(file);
        } catch {
            // noexcept
        }
    }

    public static void OpenOrCreateCustomResource(ResourceKey key)
    {
        var file = CustomFolder + key;
        if (!File.Exists(file)) {
            SetCustomResource(key, GetActiveResource(key));
        }

        Util.Run(file);
    }

    public static void OpenCustomFolder(string subFolder = "")
    {
        Util.Run(CustomFolder + subFolder);
    }

    public static void ClearCustomResources()
    {
        try {
            Directory.Delete(CustomFolder, true);
        } catch {
            // noexcept
        }

        Directory.CreateDirectory(CustomFolder);

        EmMod.Log<ResourceFetch>("cleared custom resources");
    }

#endregion
}