using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.API.Attributes;
using Cwl.Helper.Exceptions;
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

        var flagKey = flag.Value;
        var expr = optExpr.Get(">=1");

        if (!actor.IsPC) {
            return Compare(actor.GetFlagValue(flagKey), expr);
        }

        player.dialogFlags.TryAdd(flagKey, 0);
        return player.dialogFlags.TryGetValue(flagKey, out var value) && Compare(value, expr);
    }

    [CwlNodiscard]
    public static bool if_has_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var item, out var valueExpr);
        dm.RequiresActor(out var actor);

        return Compare(actor.FindAllThings(item.Value).Sum(t => t.Num), valueExpr.Get(">=1"));
    }

    [CwlNodiscard]
    public static bool if_hostility(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);
        dm.RequiresActor(out var actor);

        var match = Regex.Match(valueExpr, @"(\S*?)\s*(\S+)$");
        if (!match.Success) {
            throw new DramaActionInvokeException($"invalid expression {valueExpr}");
        }

        var expr = match.Groups[1].Value;
        if (!Enum.TryParse(match.Groups[2].Value, out Hostility hostility)) {
            throw new DramaActionInvokeException($"invalid hostility {match.Groups[2].Value}");
        }

        return Compare(actor._cints[4], $"{expr}{(int)hostility}");
    }

    [CwlNodiscard]
    public static bool if_in_party(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        return actor.IsPCParty;
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
    public static bool if_lv(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        dm.RequiresActor(out var actor);

        return Compare(actor.LV, expr);
    }

    [CwlNodiscard]
    public static bool if_race(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var race);
        dm.RequiresActor(out var actor);

        return actor.race.id == race;
    }

    [CwlNodiscard]
    public static bool if_stat(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var stat, out var valueExpr);
        dm.RequiresActor(out var actor);

        var value = stat.ToLowerInvariant() switch {
            "hunger" => actor.hunger.value,
            "burden" => actor.burden.value,
            "depression" => actor.depression.value,
            "hygiene" => actor.hygiene.value,
            "bladder" => actor.bladder.value,
            "sleepiness" => actor.sleepiness.value,
            "san" => actor.SAN.value,
            "stamina" => actor.stamina.value,
            "hp" => actor.hp,
            "mana" => actor.mana.value,
            _ => throw new DramaActionInvokeException($"invalid stat name {stat}"),
        };

        return Compare(value, valueExpr);
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