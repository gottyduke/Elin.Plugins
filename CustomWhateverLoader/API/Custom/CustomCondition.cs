using System;
using System.Collections.Generic;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Loader;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomCondition : Condition
{
    internal static readonly Dictionary<int, SourceStat.Row> Managed = [];

    public static IEnumerable<SourceStat.Row> All => Managed.Values;

    [Time]
    internal static void AddCondition(SourceStat.Row r, string qualified)
    {
        try {
            if (!SpriteSheet.dict.ContainsKey(r.alias) &&
                SpriteReplacer.dictModItems.TryGetValue(r.alias, out var icon)) {
                SpriteSheet.Add(icon.LoadSprite(name: r.alias, resizeWidth: 48, resizeHeight: 48));
            }

            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                CwlMod.Log("cwl_log_custom_ele".Loc(r.id, r.type));
            }

            Managed[r.id] = r;
        } catch (Exception ex) {
            CwlMod.Error("cwl_error_qualify_ele".Loc(r.id, r.type, ex));
            // noexcept
        }
    }
}