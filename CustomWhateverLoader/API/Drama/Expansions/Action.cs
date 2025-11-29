using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    /// <summary>
    ///     add_item(item_id, [material_alias], [lv], [count])
    /// </summary>
    public static bool add_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var id, out var material, out var lv, out var count);
        dm.RequiresActor(out var actor);

        var itemLv = lv.AsInt(-1);
        var itemCount = count.AsInt(1);
        var item = ThingGen.Create(id.Get("ash3"), ReverseId.Material(material.Get("wood")), itemLv).SetNum(itemCount);
        actor.Pick(item);

        Push(item);

        return true;
    }

    /// <summary>
    ///     apply_condition(condition_id)
    /// </summary>
    public static bool apply_condition(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var alias, out var power);
        dm.RequiresActor(out var actor);

        actor.AddCondition(alias, power.AsInt(100), true);

        return true;
    }

    /// <summary>
    ///     cure_condition(condition_id)
    /// </summary>
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

    /// <summary>
    ///     equip_item(item_id)
    /// </summary>
    public static bool equip_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        dm.RequiresActor(out var actor);

        add_item(dm, line, parameters);
        actor.body.Equip(Pop<Thing>());

        return true;
    }

    /// <summary>
    ///     destroy_item(item_id, [value_expr])
    /// </summary>
    public static bool destroy_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var item, out var valueExpr);
        dm.RequiresActor(out var actor);

        var items = actor.FindAllThings(item.Value).ToArray();

        if (!valueExpr.Provided) {
            foreach (var thing in items) {
                thing.Destroy();
            }
        } else {
            var count = Math.Max(valueExpr.AsInt(1), 0);
            foreach (var thing in items) {
                if (count <= 0) {
                    break;
                }

                if (thing.Num >= count) {
                    thing.ModNum(-count);
                } else {
                    count -= thing.Num;
                    thing.Destroy();
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     remove_condition(condition_alias)
    /// </summary>
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

    /// <summary>
    ///     join_faith(Religion)
    /// </summary>
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

    /// <summary>
    ///     join_party()
    /// </summary>
    /// <remarks>Unconditional</remarks>
    public static bool join_party(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        Sound.Play("good");
        actor.MakeAlly();

        return true;
    }
}