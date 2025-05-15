using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.Runtime;
using Cwl.Helper.Runtime.Exceptions;

// ReSharper disable InconsistentNaming

namespace Cwl.API.Drama;

public partial class DramaExpansion : DramaOutcome
{
    public static ActionCookie? Cookie { get; internal set; }

    // build and cache an external method table from other assembly
    public static bool build_ext(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var assemblyName);

        if (assemblyName != "Elin" && !CwlConfig.ExpandedActionsExternal) {
            throw new InvalidOperationException($"{CwlConfig.Dialog.ExpandedActionsAllowExternal!.Definition.Key} is disabled");
        }

        BuildActionList(assemblyName);

        return true;
    }

    // emit a call
    public static bool emit_call(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } methodName, .. { } pack]) {
            throw new DramaActionArgumentException(parameters);
        }

        if (!_built.TryGetValue(methodName, out var action) || action is null) {
            return false;
        }

        object? result;
        if (!methodName.StartsWith("ext.")) {
            if (pack.Length != action.ParameterCount) {
                var methodGroup = $"[{methodName}]({string.Join(",", parameters)})";
                throw new DramaActionInvokeException($"failed emitting call {methodGroup}\n" +
                                                     $"requires {action.ParameterCount} parameter(s).");
            }

            CwlMod.Debug<DramaExpansion>($"emit call [{methodName}]({string.Join(",", parameters)})");
            result = action.Method.FastInvokeStatic(dm, line, pack);
        } else {
            var packs = action.Method.GetParameters()
                .Select(p => Activator.CreateInstance(p.ParameterType))
                .ToArray();
            result = action.Method.FastInvokeStatic(packs);
        }

        return result is not null && (bool)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool and(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.All(expr => BuildExpression(expr)!(dm, line));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool or(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.Any(expr => BuildExpression(expr)!(dm, line));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool not(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.All(expr => !BuildExpression(expr)!(dm, line));
    }

    public static bool add_item(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var id, out var material, out var lv, out var num);
        dm.RequiresActor(out var actor);

        if (!int.TryParse(lv, out var itemLv)) {
            throw new DramaActionArgumentException(parameters);
        }

        if (!int.TryParse(num, out var itemNum)) {
            itemNum = 1;
        }

        var item = ThingGen.Create(id, ReverseId.Material(material), itemLv).SetNum(itemNum);
        actor.Pick(item);

        return item.id != "869";
    }

    public static bool affinity_check(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        dm.RequiresActor(out var actor);

        return Compare(actor._affinity, expr);
    }

    public static bool affinity_mod(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var mod);
        dm.RequiresActor(out var actor);

        if (!int.TryParse(mod, out var value)) {
            return false;
        }

        actor.ModAffinity(pc, value);

        return true;
    }

    public static bool debug_invoke(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        pc.Say($"debug_invoke : {dm.tg.Name}");

        return true;
    }

    public static bool faith_join(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var faith);
        dm.RequiresActor(out var actor);

        if (!game.religions.dictAll.TryGetValue(faith, out var religion) || !religion.CanJoin) {
            return false;
        }

        religion.JoinFaith(actor);

        return true;
    }

    public static bool faith_leave(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        actor.faith?.LeaveFaith(actor, game.religions.Eyth, Religion.ConvertType.Default);

        return true;
    }

    public static bool flag_check(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var flag, out var expr);
        dm.RequiresActor(out var actor);

        return actor.IsPC
            ? player.dialogFlags.TryGetValue(flag, out var value) && Compare(value, expr)
            : Compare(actor.GetFlagValue(flag), expr);
    }

    public static bool flag_mod(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
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

    public static bool has_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var tag);
        dm.RequiresActor(out var actor);

        return actor.source.tag.Contains(tag);
    }

    public static bool join_party(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        EClass.Sound.Play("good");
        actor.MakeAlly();

        return true;
    }

    public static bool move_tile(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var xOffset, out var yOffset);
        dm.RequiresActor(out var actor);

        var point = actor.pos.Add(new(xOffset.AsInt(0), yOffset.AsInt(0)));
        var result = actor.TryMove(point, false);

        return result == Card.MoveResult.Success;
    }

    public static bool move_zone(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var zoneName);
        dm.RequiresActor(out var target);

        if (!CustomChara.ValidateZone(zoneName, out var targetZone) || targetZone is null) {
            return false;
        }

        target.MoveZone(targetZone, new ZoneTransition {
            state = ZoneTransition.EnterState.Center,
        });

        return true;
    }

    public static bool play_anime(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var animeId);
        dm.RequiresActor(out var actor);

        if (!Enum.TryParse(animeId, out AnimeID anime)) {
            return false;
        }

        actor.PlayAnime(anime, true);

        return true;
    }

    public static bool play_effect(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var effectId);
        dm.RequiresActor(out var actor);

        actor.PlayEffect(effectId);

        return true;
    }

    public static bool play_emote(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var emoteId, out var durationStr);
        dm.RequiresActor(out var actor);

        if (!Enum.TryParse(emoteId, out Emo emote)) {
            return false;
        }

        actor.ShowEmo(emote, durationStr.AsFloat(1f), false);

        return true;
    }

    public static bool play_screen_effect(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var effectId);

        ScreenEffect.Play(effectId);

        return true;
    }

    public static bool portrait_set(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var portraitId);
        dm.RequiresPerson(out var owner);

        if (!Portrait.modPortraits.dict.ContainsKey(portraitId)) {
            return false;
        }

        owner.idPortrait = portraitId;

        return true;
    }

    public static bool portrait_reset(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresPerson(out var owner);

        owner.idPortrait = owner.chara.GetIdPortrait();

        return true;
    }
}