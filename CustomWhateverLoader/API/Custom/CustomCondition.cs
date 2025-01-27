using System;
using System.Collections.Generic;
using Cwl.Helper;
using Cwl.LangMod;

namespace Cwl.API.Custom;

public class CustomCondition : Condition
{
    internal static readonly Dictionary<int, SourceStat.Row> Managed = [];

    public static IReadOnlyCollection<SourceStat.Row> All => Managed.Values;

    internal static void AddCondition(SourceStat.Row r, string qualified)
    {
        try {
            ModSpriteReplacer.AppendSpriteSheet(r.alias, 32, 32);

            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                CwlMod.Log<CustomCondition>("cwl_log_custom_type".Loc(nameof(Condition), r.id, r.type));
            }

            SanitizePhase(r);

            Managed[r.id] = r;
        } catch (Exception ex) {
            CwlMod.ErrorWithPopup<CustomCondition>("cwl_error_qualify_type".Loc(nameof(Condition), r.id, r.type), ex);
            // noexcept
        }
    }

    private static void SanitizePhase(SourceStat.Row r)
    {
        if (r.phase.Length >= 10) {
            return;
        }

        var sanitized = new int[10];
        Array.Copy(r.phase, sanitized, r.phase.Length);
        Array.Fill(sanitized, r.phase[^1], r.phase.Length, 10 - r.phase.Length);
        r.phase = sanitized;
    }
}