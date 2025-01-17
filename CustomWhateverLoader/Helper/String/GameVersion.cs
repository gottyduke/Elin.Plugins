namespace Cwl.Helper.String;

public static class GameVersion
{
    public static string Normalized => Int().ToString();

    public static int Int()
    {
        return BaseCore.Instance.version.GetInt();
    }
}