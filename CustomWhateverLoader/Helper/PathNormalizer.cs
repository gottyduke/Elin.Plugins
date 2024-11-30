namespace Cwl.Helper;

public static class PathNormalizer
{
    public static string NormalizePath(this string path)
    {
        return path.Replace('\\', '/');
    }
}