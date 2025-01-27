using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.Helper.Runtime;

// ReSharper disable InconsistentNaming

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static ActionCookie? Cookie { get; internal set; }

    // build and cache an external method table from other assembly
    public static bool build_ext(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } assemblyName]) {
            throw new DramaActionArgumentException(parameters);
        }

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
                CwlMod.WarnWithPopup<DramaExpansion>($"failed emitting call {methodGroup}\n" +
                                                     $"requires {action.ParameterCount} parameter(s).");
                return false;
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

    public static bool affinity_check(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } id, { } expr]) {
            throw new DramaActionArgumentException(parameters);
        }

        return game.cards.globalCharas.Find(id.Trim()) is { } chara && Compare(chara._affinity, expr);
    }

    public static bool affinity_mod(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } id, { } mod]) {
            throw new DramaActionArgumentException(parameters);
        }

        if (game.cards.globalCharas.Find(id.Trim()) is not { } chara || !int.TryParse(mod, out var value)) {
            return false;
        }

        chara.ModAffinity(pc, value);
        return true;
    }

    public static bool debug_invoke(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        pc.Say($"debug_invoke : {dm.tg.Name}");
        return true;
    }

    public static bool faith_join(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } faith]) {
            throw new DramaActionArgumentException(parameters);
        }

        if (!game.religions.dictAll.TryGetValue(faith, out var religion) || !religion.CanJoin) {
            return false;
        }

        religion.JoinFaith(dm.sequence.GetActor(line["actor"]).owner.chara);
        return true;
    }

    public static bool faith_leave(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        var actor = dm.sequence.GetActor(line["actor"]).owner.chara;
        if (actor is null) {
            return false;
        }

        actor.faith?.LeaveFaith(actor, game.religions.Eyth, Religion.ConvertType.Default);
        return true;
    }

    public static bool flag_check(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } flag, { } expr]) {
            throw new DramaActionArgumentException(parameters);
        }

        return player.dialogFlags.TryGetValue(flag, out var value) && Compare(value, expr);
    }

    public static bool flag_mod(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } flag, { } expr]) {
            throw new DramaActionArgumentException(parameters);
        }

        player.dialogFlags.TryAdd(flag, 0);
        player.dialogFlags[flag] = ArithmeticModOrSet(player.dialogFlags[flag], expr);

        return true;
    }

    public static bool has_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } id, { } tag]) {
            throw new DramaActionArgumentException(parameters);
        }

        return game.cards.globalCharas.Find(id.Trim()) is { } chara && chara.source.tag.Contains(tag);
    }

    // always return true for chaining
    public static bool portrait_set(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } portraitId]) {
            throw new DramaActionArgumentException(parameters);
        }

        if (!Portrait.modPortraits.dict.ContainsKey(portraitId)) {
            return true;
        }

        if (dm.sequence.GetActor(line["actor"]) is { } actor) {
            actor.owner.idPortrait = portraitId;
        }

        return true;
    }

    public static bool portrait_reset(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (dm.sequence.GetActor(line["actor"]) is { } actor) {
            actor.owner.idPortrait = actor.owner.chara.GetIdPortrait();
        }

        return true;
    }
}