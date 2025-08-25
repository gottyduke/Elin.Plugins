namespace Cwl.LangMod;

public static class LocFormatter
{
    public static string Loc(this string id, params object?[] args)
    {
        try {
            return string.Format(id.lang(), args);
        } catch {
#if DEBUG
            throw;
#else
            return string.Join(", ", id, args);
#endif
        }
    }
}