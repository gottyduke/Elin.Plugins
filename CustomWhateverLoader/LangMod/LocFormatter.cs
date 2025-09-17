namespace Cwl.LangMod;

public static class LocFormatter
{
    public static string Loc(this string id, params object?[] args)
    {
        try {
            return string.Format(id.lang(), args);
        } catch {
            var fmt = string.Join(", ", [id, ..args]);
#if DEBUG
            CwlMod.Warn($"log fmt failure / {fmt}");
            throw;
#else
            return fmt;
#endif
        }
    }
}