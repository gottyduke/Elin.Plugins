using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static bool mod_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        dm.RequiresActor(out var actor);

        actor.ModAffinity(pc, ArithmeticDiff(actor._affinity, expr));

        return true;
    }

    public static bool mod_currency(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var currency, out var expr);
        dm.RequiresActor(out var actor);

        var held = actor.GetCurrency(currency);
        actor.ModCurrency(ArithmeticDiff(held, expr), currency);

        return true;
    }

    public static bool mod_element(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var alias, out var power);
        dm.RequiresActor(out var actor);

        actor.AddElement(alias.Get(""), power.AsInt(1));

        return true;
    }

    [CwlNodiscard]
    public static bool mod_element_exp(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias, out var expr);
        dm.RequiresActor(out var actor);

        if (actor.elements.GetOrCreateElement(alias) is not { } element) {
            return false;
        }

        actor.ModExp(element.id, ArithmeticDiff(element.vExp, expr));

        return true;
    }

    public static bool mod_fame(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);

        player.ModFame(ArithmeticDiff(player.fame, expr));

        return true;
    }

    public static bool mod_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var flag, out var optExpr);
        dm.RequiresActor(out var actor);

        var expr = optExpr.Get("=1");
        if (actor.IsPC) {
            player.dialogFlags.TryAdd(flag.Value, 0);
            player.dialogFlags[flag.Value] = ArithmeticModOrSet(player.dialogFlags[flag.Value], expr);
        } else {
            var key = flag.GetHashCode();
            actor.mapInt.TryAdd(key, 0);
            actor.mapInt[key] = ArithmeticModOrSet(actor.mapInt[key], expr);
        }

        return true;
    }

    [CwlNodiscard]
    public static bool mod_keyitem(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var keyId, out var expr);

        if (!sources.keyItems.alias.TryGetValue(keyId.Value, out var key)) {
            return false;
        }

        var keys = player.keyItems;
        keys.TryAdd(key.id, 0);

        var old = keys[key.id];
        var val = ArithmeticModOrSet(keys[key.id], expr.Get("=1"));

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