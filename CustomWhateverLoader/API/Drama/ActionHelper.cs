using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.Helper.Runtime;
using UnityEngine;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    private static readonly Dictionary<string, Func<DramaManager, Dictionary<string, string>, bool>> _cached = [];

    public static Func<DramaManager, Dictionary<string, string>, bool>? BuildAction(string expression)
    {
        if (_cached.TryGetValue(expression, out var cached)) {
            return cached;
        }

        var parse = Regex.Match(expression, @"^(?<func>\w+)(?:\((?<params>[^\)]*)\))?$");
        if (!parse.Success) {
            return null;
        }

        if (!Cached.TryGetValue(parse.Groups["func"].Value, out var method)) {
            return null;
        }

        var pack = parse.Groups["params"].Value.IsEmpty("")
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        return _cached[expression] = (dm, item) => SafeInvoke(method, dm, item, pack);
    }

    private static bool SafeInvoke(CachedMethods.MethodWrapper action,
        DramaManager dm, Dictionary<string, string> item, params string[] pack)
    {
        try {
            var result = action.Method.FastInvokeStatic(dm, item, pack);
            return result is not null && (bool)result;
        } catch (Exception ex) {
            CwlMod.Warn<DramaExpansion>($"failed emitting call [{action.Method.Name}] + {string.Join(",", pack)}\n{ex}");
            // noexcept
        }

        return false;
    }

    private static bool Compare(float lhs, string expr)
    {
        return expr.Trim() switch {
            ['>', .. { } raw] when float.TryParse(raw, out var rhs) => lhs > rhs,
            ['>', '=', .. { } raw] when float.TryParse(raw, out var rhs) => lhs >= rhs,
            ['=', .. { } raw] when float.TryParse(raw, out var rhs) => Mathf.Approximately(lhs, rhs),
            ['=', '=', .. { } raw] when float.TryParse(raw, out var rhs) => Mathf.Approximately(lhs, rhs),
            ['!', '=', .. { } raw] when float.TryParse(raw, out var rhs) => !Mathf.Approximately(lhs, rhs),
            ['<', '=', .. { } raw] when float.TryParse(raw, out var rhs) => lhs <= rhs,
            ['<', .. { } raw] when float.TryParse(raw, out var rhs) => lhs < rhs,
            _ => false,
        };
    }

    private static bool Logical(string op, Func<bool> lhs, Func<bool>? rhs = null)
    {
        rhs ??= () => false;
        return op.Trim().ToLower() switch {
            "&&" => lhs() && rhs(),
            "||" => lhs() || rhs(),
            "!" => !lhs(),
            _ => throw new ArgumentException(op),
        };
    }
}