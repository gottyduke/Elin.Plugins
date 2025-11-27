using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    /// <summary>
    ///     if_affinity(value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);
        dm.RequiresActor(out var actor);

        return Compare(actor._affinity, valueExpr);
    }

    /// <summary>
    ///     if_condition(condition_alias, [value_expr >=1])
    /// </summary>
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

    /// <summary>
    ///     if_cint(cint_id, value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_cint(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var cint, out var valueExpr);
        dm.RequiresActor(out var actor);

        var cintId = cint.AsInt(-1);
        if (cintId < 0) {
            throw new DramaActionInvokeException($"invalid cint ID '{cint}'");
        }

        return Compare(actor.GetInt(cintId), valueExpr);
    }

    /// <summary>
    ///     if_cs_get(field_or_property, [value_expr >=0])
    /// </summary>
    /// <remarks>We don't cache this</remarks>
    [CwlNodiscard]
    public static bool if_cs_get(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var memberName, out var optExpr);
        dm.RequiresActor(out var actor);

        var valueExpr = optExpr.Get(">=0");
        var member = actor.GetFieldValue(memberName.Value) ?? actor.GetPropertyValue(memberName.Value);
        if (member is null) {
            throw new DramaActionInvokeException($"cs member '{memberName.Value}' does not exist");
        }

        return member switch {
            int intVal => Compare(intVal, valueExpr),
            float floatVal => Compare(floatVal, valueExpr),
            double doubleVal => Compare((float)doubleVal, valueExpr),
            bool boolVal => boolVal,
            string stringVal => string.Equals(stringVal, valueExpr, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    /// <summary>
    ///     if_currency(Currency, value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_currency(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var currency, out var valueExpr);
        dm.RequiresActor(out var actor);

        return Compare(actor.GetCurrency(currency), valueExpr);
    }

    /// <summary>
    ///     if_element(element_alias, value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_element(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias, out var valueExpr);
        dm.RequiresActor(out var actor);

        return actor.HasElement(alias) && Compare(actor.elements.GetElement(alias).Value, valueExpr);
    }

    /// <summary>
    ///     if_faith(Religion, [value_expr >=0])
    /// </summary>
    [CwlNodiscard]
    public static bool if_faith(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var faithId, out var optExpr);
        dm.RequiresActor(out var actor);

        var faith = actor.faith;

        return faith.id == faithId.Value && Compare(faith.giftRank, optExpr.Get(">=0"));
    }

    /// <summary>
    ///     if_fame(value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_fame(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);

        return Compare(player.fame, valueExpr);
    }

    /// <summary>
    ///     if_flag(flag_key, [value_expr >=1])
    /// </summary>
    [CwlNodiscard]
    public static bool if_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var flag, out var optExpr);
        dm.RequiresActor(out var actor);

        var flagKey = flag.Value;
        var valueExpr = optExpr.Get(">=1");

        if (!actor.IsPC) {
            return Compare(actor.GetFlagValue(flagKey), valueExpr);
        }

        player.dialogFlags.TryAdd(flagKey, 0);
        return player.dialogFlags.TryGetValue(flagKey, out var value) && Compare(value, valueExpr);
    }

    /// <summary>
    ///     if_has_item(item_id, [value_expr >=1])
    /// </summary>
    [CwlNodiscard]
    public static bool if_has_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var item, out var optExpr);
        dm.RequiresActor(out var actor);

        return Compare(actor.FindAllThings(item.Value).Sum(t => t.Num), optExpr.Get(">=1"));
    }

    /// <summary>
    ///     if_hostility(Hostility_value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_hostility(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);
        dm.RequiresActor(out var actor);

        var match = Regex.Match(valueExpr, "^(?<op>>=|<=|>|<|=|!=|==)?(?<hostility>.+)$");
        if (!match.Success) {
            throw new DramaActionInvokeException($"invalid expression {valueExpr}");
        }

        var expr = match.Groups["op"].Value;
        if (!Enum.TryParse(match.Groups["hostility"].Value, true, out Hostility hostility)) {
            throw new DramaActionInvokeException($"invalid hostility {match.Groups["hostility"].Value}");
        }

        return Compare(actor._cints[4], $"{expr}{(int)hostility}");
    }

    /// <summary>
    ///     if_in_party()
    /// </summary>
    [CwlNodiscard]
    public static bool if_in_party(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        return actor.IsPCParty;
    }

    /// <summary>
    ///     if_keyitem(keyitem_id, [value_expr >0])
    /// </summary>
    [CwlNodiscard]
    public static bool if_keyitem(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var keyId, out var optExpr);

        return sources.keyItems.alias.TryGetValue(keyId.Value, out var key) &&
               player.keyItems.TryGetValue(key.id, out var keyCount) && Compare(keyCount, optExpr.Get(">0"));
    }

    /// <summary>
    ///     if_lv(value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool if_lv(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        dm.RequiresActor(out var actor);

        return Compare(actor.LV, expr);
    }

    /// <summary>
    ///     if_race(race_id)
    /// </summary>
    [CwlNodiscard]
    public static bool if_race(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var race);
        dm.RequiresActor(out var actor);

        return actor.race.id == race;
    }

    /// <summary>
    ///     if_stat(stat_name, value_expr)
    /// </summary>
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

    /// <summary>
    ///     if_tag(tag)
    /// </summary>
    [CwlNodiscard]
    public static bool if_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var tag);
        dm.RequiresActor(out var actor);

        return actor.source.tag.Contains(tag);
    }

    /// <summary>
    ///     if_zone(zone_id, [zone_level 0])
    /// </summary>
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