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

        actor.AddElement(alias.Get(""), power.Get("1").AsInt(1));

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

    public static bool mod_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var flag, out var expr);
        dm.RequiresActor(out var actor);

        if (actor.IsPC) {
            player.dialogFlags.TryAdd(flag, 0);
            player.dialogFlags[flag] = ArithmeticModOrSet(player.dialogFlags[flag], expr);
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
        keys[key.id] = ArithmeticModOrSet(keys[key.id], expr.Get("1"));

        return true;
    }
}