using System.Collections.Generic;
using Cwl.Helper;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomCondition : Condition
{
    internal static readonly Dictionary<int, SourceStat.Row> Managed = [];

    public static IReadOnlyCollection<SourceStat.Row> All => Managed.Values;

    [Time]
    internal static void AddCondition(SourceStat.Row r, string qualified)
    {
        try {
            ModSpriteReplacer.AppendSpriteSheet(r.alias, 32, 32);

            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                CwlMod.Log("cwl_log_custom_type".Loc(nameof(Condition), r.id, r.type));
            }

            Managed[r.id] = r;
        } catch {
            CwlMod.Error("cwl_error_qualify_type".Loc(nameof(Condition), r.id, r.type));
            // noexcept
        }
    }
}