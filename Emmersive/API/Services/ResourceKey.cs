using System.IO;

namespace Emmersive.API.Services;

public class ResourceKey(string path)
{
    public string ResourcePath => field ??= path.NormalizePath().SanitizeDirectoryName();

    public static ResourceKey operator +(ResourceKey lhs, ResourceKey rhs)
    {
        return new(Path.Combine(lhs.ResourcePath, rhs.ResourcePath));
    }

    public static ResourceKey operator +(ResourceKey lhs, string rhs)
    {
        return new(Path.Combine(lhs.ResourcePath, rhs));
    }

    public static implicit operator ResourceKey(string path)
    {
        return new(path);
    }

    public static implicit operator string(ResourceKey key)
    {
        return key.ResourcePath;
    }

    public override string ToString()
    {
        return ResourcePath;
    }
}