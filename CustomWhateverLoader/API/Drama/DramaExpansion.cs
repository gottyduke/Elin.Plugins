using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.Helper.Runtime;

// ReSharper disable InconsistentNaming

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static ActionCookie? Cookie { get; internal set; }

    public static bool emit_call(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } methodName, .. { } pack]) {
            return false;
        }

        var unsafeCall = methodName.StartsWith("ext.");
        if (unsafeCall) {
            BuildActionList(true);
        }

        if (!_built.TryGetValue(methodName, out var action)) {
            return false;
        }

        object? result;
        if (!unsafeCall) {
            if (pack.Length != action.ParameterCount) {
                CwlMod.Warn<DramaExpansion>($"failed emitting call [{methodName}]({string.Join(",", parameters)})\n" +
                                            $"requires {action.ParameterCount} parameter(s).");
                return false;
            }

            CwlMod.Debug<DramaExpansion>($"emit call [{methodName}]({string.Join(",", parameters)})");
            result = action.Method.FastInvokeStatic(dm, line, pack);
        } else {
            // can throw
            result = action.Method.FastInvokeStatic();
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

    public static bool check_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        return parameters is [{ } id, { } expr] &&
               game.cards.globalCharas.Find(id.Trim()) is { } chara &&
               Compare(chara._affinity, expr);
    }

    public static bool debug_invoke(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        pc.Say($"debug_invoke : {dm.tg.Name}");
        return true;
    }

    public static bool has_tag(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        return parameters is [{ } id, { } tag] &&
               game.cards.globalCharas.Find(id.Trim()) is { } chara &&
               chara.source.tag.Contains(tag);
    }

    public static bool join_faith(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } faith] ||
            !game.religions.dictAll.TryGetValue(faith, out var religion) ||
            !religion.CanJoin) {
            return false;
        }

        religion.JoinFaith(pc);
        return true;
    }

    public static bool mod_affinity(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } id, { } mod] ||
            game.cards.globalCharas.Find(id.Trim()) is not { } chara ||
            !int.TryParse(mod, out var value)) {
            return false;
        }

        chara.ModAffinity(pc, value);
        return true;
    }

    public record ActionCookie(DramaManager? Dm, Dictionary<string, string>? Line);
}