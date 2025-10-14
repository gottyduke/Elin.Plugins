using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using UnityEngine;

namespace Emmersive.Helper;

public class ResourceFetch
{
    private const string DefaultResource = "Emmersive.package.LangMod.";

    // holds user custom edits
    private static readonly Dictionary<string, string> _activeResources = [];

    // holds active data exchanges
    public static readonly GameIOProcessor.GameIOContext Context = GameIOProcessor.GetPersistentModContext("Emmersive")!;

    public static string CustomFolder { get; private set; } = Path.Combine(Application.persistentDataPath, "Emmersive/Custom");

    public static IEnumerable<ResourceDescriptor> GetAvailableResources(string path)
    {
        using var _ = PackageIterator.AddTempLookupPathsEx(ModInfo.Guid, CustomFolder);
        return PackageIterator.GetRelocatedFilesFromPackageEx(path)
            .Select(fm => new ResourceDescriptor(fm.Item1, fm.Item2?.title ?? "Custom"));
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

    [CwlPostSave]
    internal static void SaveActiveResources()
    {
        Context.Save(_activeResources, "active_resources");
    }

    public sealed record ResourceDescriptor(FileInfo Provider, string PackageName = "");

#region Active Resource

    public static string GetActiveResource(string path, bool autoSet = true)
    {
        if (_activeResources.TryGetValue(path, out var resource)) {
            return resource;
        }

        if (!autoSet) {
            return "";
        }

        var fallbackResource = GetAvailableResources(path).LastOrDefault();
        resource = fallbackResource is not null
            ? File.ReadAllText(fallbackResource.Provider.FullName)
            : "";

        SetActiveResource(path, resource);

        return resource;
    }

    public static void SetActiveResource(string path, string content)
    {
        _activeResources[path] = content;
        EmMod.Log<ResourceFetch>($"set active resource {path}");
    }

    public static void ClearActiveResources()
    {
        _activeResources.Clear();
        EmMod.Log<ResourceFetch>("cleared active resources");
    }

#endregion

#region Custom Resource

    public static void SetCustomPath(string path)
    {
        CustomFolder = path;
        Directory.CreateDirectory(CustomFolder);
    }

    public static string GetCustomResource(string path)
    {
        var file = Path.Combine(CustomFolder, path);
        return !File.Exists(file) ? "" : File.ReadAllText(file);
    }

    public static void SetCustomResource(string path, string content)
    {
        var file = new FileInfo(Path.Combine(CustomFolder, path));
        Directory.CreateDirectory(Path.GetDirectoryName(file.FullName)!);
        File.WriteAllText(file.FullName, content);
        EmMod.Log<ResourceFetch>($"set custom resource {path}");
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