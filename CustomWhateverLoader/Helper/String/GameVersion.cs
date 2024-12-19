namespace Cwl.Helper.String;

internal static class GameVersion
{
    internal static string Normalized => Int().ToString();

    internal static int Int()
    {
        return BaseCore.Instance.version.GetInt();
    }
}