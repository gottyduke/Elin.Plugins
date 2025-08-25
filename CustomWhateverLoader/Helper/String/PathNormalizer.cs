using System;
using System.IO;

namespace Cwl.Helper.String;

public static class PathNormalizer
{
    private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();
    public static ReadOnlySpan<char> InvalidPathChars => _invalidPathChars;

    extension(string path)
    {
        public string NormalizePath()
        {
            return path.Replace('\\', '/');
        }

        public bool IsInvalidPath()
        {
            return path.IndexOfAny(InvalidPathChars) != -1;
        }
    }
}