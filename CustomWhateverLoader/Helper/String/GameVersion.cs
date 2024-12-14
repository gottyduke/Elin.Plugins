namespace Cwl.Helper.String;

internal static class GameVersion
{
    internal static string Normalized => BaseCore.Instance.version.GetInt().ToString();
}