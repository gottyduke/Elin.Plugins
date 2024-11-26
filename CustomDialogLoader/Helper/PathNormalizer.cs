namespace Cwl.Helper;

internal static class PathNormalizer
{
    internal static string NormalizePath(this string path)
    {
        return path.Replace('\\', '/');
    }
}