using System;
using System.Collections.Generic;
using Cwl.Helper.Runtime;
using UnityEngine;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    private static bool SafeInvoke(ActionWrapper action, DramaManager dm, Dictionary<string, string> item, params string[] pack)
    {
        try {
            var result = action.Method.FastInvokeStatic(dm, item, pack);
            return result is not null && (bool)result;
        } catch (Exception ex) {
            CwlMod.Warn<DramaExpansion>($"failed emitting call [{action.Method.Name}]({string.Join(",", pack)})\n{ex}");
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
}