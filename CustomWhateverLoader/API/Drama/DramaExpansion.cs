using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.Helper;
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
        actor.AddThing(item);

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

        return player.dialogFlags.TryGetValue(flag, out var value) && Compare(value, expr);
    }

    public static bool flag_mod(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var flag, out var expr);

        player.dialogFlags.TryAdd(flag, 0);
        player.dialogFlags[flag] = ArithmeticModOrSet(player.dialogFlags[flag], expr);

        return true;
    }

    public static bool has_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var id, out var tag);

        return game.cards.globalCharas.Find(id.Trim()) is { } chara && chara.source.tag.Contains(tag);
    }

    public static bool join_party(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresActor(out var actor);

        AddTempTalk(dm, actor.GetTalkText("hired"));
        EClass.Sound.Play("good");
        actor.MakeAlly();

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

    internal static bool portrait_reset(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        dm.RequiresPerson(out var owner);

        owner.idPortrait = owner.chara.GetIdPortrait();
        return true;
    }
}