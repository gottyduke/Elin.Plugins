using System;
using System.IO;

namespace Cwl.Helper.String;

public static class PathTruncation
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

        public string ShortPath()
        {
            return ShortPath(new FileInfo(path));
        }
    }

    extension(FileInfo file)
    {
        public string ShortPath()
        {
            var owner = file.Directory!.Parent!.Parent!.Parent;
            return file.FullName[(owner!.Parent!.FullName.Length + 1)..].NormalizePath();
        }
    }
}