using System;
using System.Collections.Generic;
using System.IO;

namespace Cwl.Helper.String;

public static class PathTruncation
{
    private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();
    private static readonly char[] _invalidFileChars = Path.GetInvalidFileNameChars();
    public static IEqualityComparer<string> PathComparer => field ??= new PathStringComparer();

    extension(string path)
    {
        public string NormalizePath()
        {
            return path.Replace('\\', '/');
        }

        public bool IsInvalidPath()
        {
            return path.IndexOfAny(_invalidPathChars) != -1;
        }

        public bool IsInvalidFileName()
        {
            return path.IndexOfAny(_invalidFileChars) != -1;
        }

        public string SanitizePath(char replacement = '_')
        {
            using var sb = StringBuilderPool.Get();

            foreach (var c in path) {
                sb.Append(_invalidPathChars.IndexOf(c) != -1 ? replacement : c);
            }

            return sb.ToString();
        }

        public string SanitizeFileName(char replacement = '_')
        {
            using var sb = StringBuilderPool.Get();

            foreach (var c in path) {
                sb.Append(_invalidFileChars.IndexOf(c) != -1 ? replacement : c);
            }

            return sb.ToString();
        }

        public string ShortPath()
        {
            return ShortPath(new FileInfo(path));
        }
    }

    extension(FileInfo file)
    {
        public string ShortPath()
        {
            const int trimParents = 4;

            var dir = file.Directory;
            for (var i = 0; i < trimParents; ++i) {
                dir = dir?.Parent;

                if (dir is null) {
                    return file.FullName.NormalizePath();
                }
            }

            var parents = dir!.FullName;
            if (!parents.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                parents += Path.DirectorySeparatorChar;
            }

            var shortPath = file.FullName.StartsWith(parents, StringComparison.InvariantCultureIgnoreCase)
                ? file.FullName[parents.Length..]
                : file.FullName;
            return shortPath.NormalizePath();
        }
    }

    private sealed class PathStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string? lhs, string? rhs)
        {
            if (ReferenceEquals(lhs, rhs)) {
                return true;
            }

            if (lhs is null || rhs is null) {
                return false;
            }

            return string.Equals(NormalizePath(lhs), NormalizePath(rhs), StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string? obj)
        {
            return obj is null
                ? 0
                : NormalizePath(obj).ToLowerInvariant().GetHashCode();
        }
    }
}