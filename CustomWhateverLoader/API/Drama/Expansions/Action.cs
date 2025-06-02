using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static bool add_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var id, out var material, out var lv, out var num);
        dm.RequiresActor(out var actor);

        if (!int.TryParse(lv.Get("-1"), out var itemLv)) {
            itemLv = -1;
        }

        if (!int.TryParse(num.Get("1"), out var itemNum)) {
            itemNum = 1;
        }

        var item = ThingGen.Create(id.Get("ash3"), ReverseId.Material(material.Get("wood")), itemLv).SetNum(itemNum);
        actor.Pick(item);

        return true;
    }

    // TODO doc
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

    [CwlNodiscard]
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