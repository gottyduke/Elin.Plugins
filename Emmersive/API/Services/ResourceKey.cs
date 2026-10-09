using System;
using System.IO;

namespace Emmersive.API.Services;

public sealed class ResourceKey(string path) : IEquatable<ResourceKey>
{
    public string ResourcePath => field ??= path.NormalizePath().SanitizeDirectoryName();

    public bool Equals(ResourceKey? other)
    {
        return other is not null && StringComparer.OrdinalIgnoreCase.Equals(ResourcePath, other.ResourcePath);
    }

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

    public override bool Equals(object? obj)
    {
        return obj is ResourceKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(ResourcePath);
    }

    public override string ToString()
    {
        return ResourcePath;
    }
}