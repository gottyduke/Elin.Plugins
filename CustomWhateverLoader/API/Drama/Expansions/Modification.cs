using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;

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
        actor.SetFlagValue(flag.Value, ArithmeticModOrSet(actor.GetFlagValue(flag.Value), valueExpr));

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