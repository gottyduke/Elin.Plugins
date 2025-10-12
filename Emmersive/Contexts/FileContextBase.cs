using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.FileUtil;

namespace Emmersive.Contexts;

public abstract class FileContextBase<T> : ContextProviderBase
{
    public static readonly Dictionary<string, T> Overrides = [];
    public static ILookup<string, T>? Lookup { get; protected set; }

    protected abstract T? LoadFromFile(FileInfo file);

    protected IEnumerable<T> LoadAllContexts(string path)
    {
        return PackageIterator.GetRelocatedDirsFromPackage(path)
            .SelectMany(d => d.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            .Select(LoadFromFile)
            .OfType<T>();
    }

    public static T? GetContext(string key)
    {
        if (!Overrides.TryGetValue(key, out var promptOverride)) {
            promptOverride = Overrides[key] = Lookup![key].LastOrDefault();
        }

        return promptOverride;
    }

    public static void SetOverride(string key, T value)
    {
        Overrides[key] = value;
    }
}