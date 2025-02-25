using System;
using System.Collections.Generic;
using Cwl.Helper.Runtime;
using UnityEngine;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static void AddTempTalk(DramaManager dm, string text, string? jump = null)
    {
        var talkEvent = new DramaEventTalk {
            idActor = "tg",
            idJump = jump ?? dm.sequence.lastlastStep.IsEmpty("end"),
            text = text,
            temp = true,
            sequence = dm.sequence,
        };

        dm.lastTalk = talkEvent;
        dm.AddEvent(talkEvent);
        dm.sequence.tempEvents.Add(dm.lastTalk);
    }

    public static bool Compare(float lhs, string expr)
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

    public static float ArithmeticModOrSet(float lhs, string expr)
    {
        return expr.Trim() switch {
            ['+', .. { } raw] when float.TryParse(raw, out var rhs) => lhs + rhs,
            ['+', '+'] => lhs + 1,
            ['-', .. { } raw] when float.TryParse(raw, out var rhs) => lhs - rhs,
            ['-', '-'] => lhs - 1,
            ['*', .. { } raw] when float.TryParse(raw, out var rhs) => lhs * rhs,
            ['x', .. { } raw] when float.TryParse(raw, out var rhs) => lhs * rhs,
            ['/', .. { } raw] when float.TryParse(raw, out var rhs) => lhs / rhs,
            { } raw when float.TryParse(raw, out var rhs) => rhs,
            _ => lhs,
        };
    }

    public static int ArithmeticModOrSet(int lhs, string expr)
    {
        return (int)ArithmeticModOrSet((float)lhs, expr);
    }

    private static bool SafeInvoke(ActionWrapper action, DramaManager dm, Dictionary<string, string> item, params string[] pack)
    {
        try {
            var result = action.Method.FastInvokeStatic(dm, item, pack);
            return result is not null && (bool)result;
        } catch (Exception ex) {
            var methodGroup = $"[{action.Method.Name}]({string.Join(",", pack)})";
            CwlMod.WarnWithPopup<DramaExpansion>($"call failure: {methodGroup}\n{ex.Message}", ex);
            // noexcept
        }

        return false;
    }
}