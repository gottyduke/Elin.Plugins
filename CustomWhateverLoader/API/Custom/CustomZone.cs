using System.Collections.Generic;
using Cwl.LangMod;
using Cwl.Loader;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomZone : Zone
{
    internal static readonly Dictionary<string, SourceZone.Row> Managed = [];

    public static IEnumerable<SourceZone.Row> All => Managed.Values;

    [Time]
    public static void AddZone(SourceZone.Row r, string qualified)
    {
        try {
            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                CwlMod.Log("cwl_log_custom_type".Loc(nameof(Zone), r.id, r.type));
            }

            Managed[r.id] = r;
        } catch {
            CwlMod.Error("cwl_error_qualify_type".Loc(nameof(Zone), r.id, r.type));
            // noexcept
        }
    }
}