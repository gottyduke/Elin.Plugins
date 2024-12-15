namespace Cwl.LangMod;

public static class LocFormatter
{
    public static string Loc(this string id, params object[] args)
    {
        return string.Format(id.lang(), args);
    }
}