using System.IO;
using System.Text;

namespace Cwl.Helper.String;

public static class PathExt
{
    private static char[]? _invalidChars;
    public static char[] InvalidChars => _invalidChars ??= [..Path.GetInvalidFileNameChars(), ..Path.GetInvalidPathChars()];

    public static string NormalizePath(this string path)
    {
        var index = path.IndexOfAny(InvalidChars);
        if (index < 0) {
            return path;
        }

        var sb = new StringBuilder(path);
        while (index >= 0) {
            sb[index] = sb[index] != '\\' ? '_' : '/';
            index = path.IndexOfAny(InvalidChars, index + 1);
        }

        return sb.ToString();
    }

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