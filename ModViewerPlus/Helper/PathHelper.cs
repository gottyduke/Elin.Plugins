using System.IO;

namespace ViewerMinus.Helper;

public static class PathHelper
{
    public static string ShortFilename(this FileInfo file)
    {
        return file.Name[..^file.Extension.Length];
    }
}