using System.Collections.Generic;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.Runtime.Exceptions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static bool add_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtleast(1);
        parameters.RequiresOpt(out var id, out var material, out var lv, out var num);
        dm.RequiresActor(out var actor);

        if (!int.TryParse(lv.Get("-1"), out var itemLv)) {
            throw new DramaActionArgumentException(parameters);
        }

        if (!int.TryParse(num.Get("1"), out var itemNum)) {
            itemNum = 1;
        }

        var item = ThingGen.Create(id.Get("ash3"), ReverseId.Material(material.Get("wood")), itemLv).SetNum(itemNum);
        actor.Pick(item);

        return true;
    }

    public static bool add_element(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtleast(1);
        parameters.RequiresOpt(out var alias, out var power);
        dm.RequiresActor(out var actor);

        actor.AddElement(alias.Get(""), power.Get("1").AsInt(1));

        return true;
    }

    public static bool add_temp_talk(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var topic);
        dm.RequiresActor(out var actor);

        AddTempTalk(dm, topic, line["actor"], line["jump"]);

        return true;
    }

    public static bool apply_condition(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias, out var power);
        dm.RequiresActor(out var actor);

        actor.AddCondition(alias, power.AsInt(100), true);

        return true;
    }

    // nodiscard
    public static bool cure_condition(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias);
        dm.RequiresActor(out var actor);

        foreach (var condition in actor.conditions) {
            if (condition.source.alias != alias) {
                continue;
            }

            condition.value -= int.MaxValue;
            if (condition.value <= 0) {
                condition.Kill();
            }

            return true;
        }

        return false;
    }

    public static bool remove_condition(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias);
        dm.RequiresActor(out var actor);

        foreach (var condition in actor.conditions) {
            if (condition.source.alias == alias) {
                condition.Kill();
            }
        }

        return true;
    }

    public static bool mod_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var mod);
        dm.RequiresActor(out var actor);

        if (!int.TryParse(mod, out var value)) {
            return false;
        }

        actor.ModAffinity(pc, value);

        return true;
    }

    public static bool mod_flag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtleast(1);
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

    // nodiscard
    public static bool mod_keyitem(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtleast(1);
        parameters.RequiresOpt(out var keyId, out var expr);

        if (!sources.keyItems.alias.TryGetValue(keyId.Value, out var key)) {
            return false;
        }

        var keys = player.keyItems;
        keys.TryAdd(key.id, 0);
        keys[key.id] = ArithmeticModOrSet(keys[key.id], expr.Get("1"));

        return true;
    }

    public static bool join_faith(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresOpt(out var faithId);
        dm.RequiresActor(out var actor);

        if (faithId.Provided) {
            if (!game.religions.dictAll.TryGetValue(faithId.Value, out var religion) || !religion.CanJoin) {
                return false;
            }

            religion.JoinFaith(actor);
        } else {
            actor.faith?.LeaveFaith(actor, game.religions.Eyth, Religion.ConvertType.Default);
        }

        return true;
    }

    public static bool join_party(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        Sound.Play("good");
        actor.MakeAlly();

        return true;
    }
}