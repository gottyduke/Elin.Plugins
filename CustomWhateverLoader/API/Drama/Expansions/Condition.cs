﻿using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    [CwlNodiscard]
    public static bool if_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);
        dm.RequiresActor(out var actor);

        return Compare(actor._affinity, valueExpr);
    }

    [CwlNodiscard]
    public static bool if_condition(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var alias, out var optExpr);
        dm.RequiresActor(out var actor);

        foreach (var condition in actor.conditions) {
            if (condition.source.alias == alias.Value) {
                return Compare(condition.value, optExpr.Get(">=1"));
            }
        }

        return false;
    }

    [CwlNodiscard]
    public static bool if_currency(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var currency, out var expr);
        dm.RequiresActor(out var actor);

        return Compare(actor.GetCurrency(currency), expr);
    }

    [CwlNodiscard]
    public static bool if_element(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias, out var valueExpr);
        dm.RequiresActor(out var actor);

        return actor.HasElement(alias) && Compare(actor.elements.GetElement(alias).Value, valueExpr);
    }

    [CwlNodiscard]
    public static bool if_faith(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var faithId, out var optExpr);
        dm.RequiresActor(out var actor);

        var faith = actor.faith;

        return faith.id == faithId.Value && Compare(faith.giftRank, optExpr.Get(">=0"));
    }

    [CwlNodiscard]
    public static bool if_fame(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);

        return Compare(player.fame, expr);
    }

    [CwlNodiscard]
    public static bool if_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var flag, out var optExpr);
        dm.RequiresActor(out var actor);

        var flagVal = flag.Value;
        var expr = optExpr.Get(">=1");

        return actor.IsPC
            ? player.dialogFlags.TryGetValue(flagVal, out var value) && Compare(value, expr)
            : Compare(actor.GetFlagValue(flagVal), expr);
    }

    [CwlNodiscard]
    public static bool if_keyitem(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var keyId, out var optExpr);

        return sources.keyItems.alias.TryGetValue(keyId.Value, out var key) &&
               player.keyItems.TryGetValue(key.id, out var keyCount) && Compare(keyCount, optExpr.Get(">0"));
    }

    [CwlNodiscard]
    public static bool if_race(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var race);
        dm.RequiresActor(out var actor);

        return actor.race.id == race;
    }

    [CwlNodiscard]
    public static bool if_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var tag);
        dm.RequiresActor(out var actor);

        return actor.source.tag.Contains(tag);
    }

    [CwlNodiscard]
    public static bool if_zone(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var zoneId, out var optLevel);
        dm.RequiresActor(out var actor);

        var zone = actor.currentZone;

        return zone.id == zoneId.Value && (!optLevel.Provided || zone.lv.ToString() == optLevel.Get("0"));
    }
}