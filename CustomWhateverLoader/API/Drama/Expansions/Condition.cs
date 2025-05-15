using System.Collections.Generic;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    // nodiscard
    public static bool if_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        dm.RequiresActor(out var actor);

        return Compare(actor._affinity, expr);
    }

    // nodiscard
    public static bool if_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresMoreThan(1);
        parameters.RequiresOpt(out var flag, out var optExpr);
        dm.RequiresActor(out var actor);

        var flagVal = flag.Value;
        var expr = optExpr.Get(">=1");

        return actor.IsPC
            ? player.dialogFlags.TryGetValue(flagVal, out var value) && Compare(value, expr)
            : Compare(actor.GetFlagValue(flagVal), expr);
    }

    // nodiscard
    public static bool if_condition(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresMoreThan(1);
        parameters.RequiresOpt(out var alias, out var optExpr);
        dm.RequiresActor(out var actor);

        foreach (var condition in actor.conditions) {
            if (condition.source.alias == alias.Value) {
                return Compare(condition.value, optExpr.Get(">=1"));
            }
        }

        return false;
    }

    // nodiscard
    public static bool if_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var tag);
        dm.RequiresActor(out var actor);

        return actor.source.tag.Contains(tag);
    }

    // nodiscard
    public static bool if_location(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresMoreThan(1);
        parameters.RequiresOpt(out var zoneId, out var optLevel);
        dm.RequiresActor(out var actor);

        var zone = actor.currentZone;

        return zone.id == zoneId.Value && (!optLevel.Provided || zone.lv.ToString() == optLevel.Get("0"));
    }
}