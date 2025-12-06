using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    /// <summary>
    ///     mod_affinity(value_expr)
    /// </summary>
    /// <remarks>Note the assignment is not exact 1:1</remarks>
    public static bool mod_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);
        dm.RequiresActor(out var actor);

        actor.ModAffinity(pc, ArithmeticDiff(actor._affinity, valueExpr));

        return true;
    }

    /// <summary>
    ///     mod_cs_set(field_or_property, value_expr)
    /// </summary>
    public static bool mod_cs_set(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var memberName, out var valueExpr);
        dm.RequiresActor(out var actor);

        Action<object> setter;
        object memberValue;

        var member = actor.GetType().GetCachedField(memberName) as MemberInfo ??
                     actor.GetType().GetProperty(memberName, AccessTools.all & ~BindingFlags.Static);
        switch (member) {
            case FieldInfo field:
                setter = v => field.SetValue(actor, v);
                memberValue = field.GetValue(actor);
                break;
            case PropertyInfo property:
                setter = v => property.SetValue(actor, v);
                memberValue = property.GetValue(actor);
                break;
            default:
                throw new DramaActionInvokeException($"cs member '{memberName}' does not exist");
        }

        switch (memberValue) {
            case int i:
                setter(ArithmeticModOrSet(i, valueExpr));
                break;
            case float f:
                setter(ArithmeticModOrSet(f, valueExpr));
                break;
            default:
                var type = memberValue?.GetType() ?? throw new DramaActionInvokeException("cs member value is null");
                setter(valueExpr.AsTypeOf(type));
                break;
        }

        return true;
    }

    /// <summary>
    ///     mod_currency(Currency, value_expr)
    /// </summary>
    public static bool mod_currency(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var currency, out var valueExpr);
        dm.RequiresActor(out var actor);

        var held = actor.GetCurrency(currency);
        actor.ModCurrency(ArithmeticDiff(held, valueExpr), currency);

        return true;
    }

    /// <summary>
    ///     mod_element(element_alias, [power 1])
    /// </summary>
    public static bool mod_element(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var alias, out var power);
        dm.RequiresActor(out var actor);

        actor.AddElement(alias.Get(""), power.AsInt(1));

        return true;
    }

    /// <summary>
    ///     mod_element_exp(element_alias, value_expr)
    /// </summary>
    [CwlNodiscard]
    public static bool mod_element_exp(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias, out var valueExpr);
        dm.RequiresActor(out var actor);

        if (actor.elements.GetOrCreateElement(alias) is not { } element) {
            return false;
        }

        actor.ModExp(element.id, ArithmeticDiff(element.vExp, valueExpr));

        return true;
    }

    /// <summary>
    ///     mod_fame(value_expr)
    /// </summary>
    public static bool mod_fame(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var valueExpr);

        player.ModFame(ArithmeticDiff(player.fame, valueExpr));

        return true;
    }

    /// <summary>
    ///     mod_flag(flag_key, [value_expr =1])
    /// </summary>
    public static bool mod_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var flag, out var optExpr);
        dm.RequiresActor(out var actor);

        var valueExpr = optExpr.Get("=1");
        if (actor.IsPC) {
            player.dialogFlags.TryAdd(flag.Value, 0);
            player.dialogFlags[flag.Value] = ArithmeticModOrSet(player.dialogFlags[flag.Value], valueExpr);
        } else {
            var key = flag.GetHashCode();
            actor.mapInt.TryAdd(key, 0);
            actor.mapInt[key] = ArithmeticModOrSet(actor.mapInt[key], valueExpr);
        }

        return true;
    }

    /// <summary>
    ///     mod_keyitem(keyitem_id, [value_expr =1])
    /// </summary>
    [CwlNodiscard]
    public static bool mod_keyitem(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var keyId, out var optExpr);

        if (!sources.keyItems.alias.TryGetValue(keyId.Value, out var key)) {
            return false;
        }

        var keys = player.keyItems;
        keys.TryAdd(key.id, 0);

        var old = keys[key.id];
        var val = ArithmeticModOrSet(keys[key.id], optExpr.Get("=1"));

        if (old < val) {
            SE.Play("keyitem");
            Msg.Say("get_keyItem", key.GetName());
        } else if (old > val) {
            SE.Play("keyitem_lose");
            Msg.Say("lose_keyItem", key.GetName());
        }

        keys[key.id] = val;

        return true;
    }
}