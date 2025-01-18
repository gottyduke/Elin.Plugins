using System.IO;

namespace Cwl.Helper.String;

public static class PathTruncation
{
    public static string ShortPath(this string path)
    {
        return ShortPath(new FileInfo(path));
    }
    
    public static string ShortPath(this FileInfo file)
    {
        var owner = file.Directory!.Parent!.Parent!.Parent;
        return file.FullName[(owner!.Parent!.FullName.Length + 1)..].NormalizePath();
    }
}