namespace Cwl.Helper.String;

public static class GameVersion
{
    public static string Normalized => Int().ToString();

    public static int Int()
    {
        return BaseCore.Instance.version.GetInt();
    }

    public static bool IsBelow(int fullVersion)
    {
        return Int() < fullVersion;
    }

    public static bool IsSameOrBelow(int fullVersion)
    {
        return Int() <= fullVersion;
    }

    public static bool IsBelow(int major, int minor, int batch)
    {
        return IsBelow(major * 1000000 + minor * 1000 + batch);
    }

    public static bool IsSameOrBelow(int major, int minor, int batch)
    {
        return IsSameOrBelow(major * 1000000 + minor * 1000 + batch);
    }
}