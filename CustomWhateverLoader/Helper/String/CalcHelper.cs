using System;
using System.Globalization;
using System.Linq;
using AK;
using Cwl.LangMod;
using UnityEngine;

namespace Cwl.Helper.String;

public static class CalcHelper
{
    extension(string expression)
    {
        public bool TryEvaluate(out int result, object? args = null)
        {
            result = 0;

            try {
                var calc = expression.GetExpression(args);
                result = Convert.ToInt32(calc.Evaluate(), CultureInfo.InvariantCulture);
                return true;
            } catch (Exception ex) {
                CwlMod.Warn<Cal>("cwl_error_failure".Loc($"{expression}\n{ex.Message}"));
                return false;
                // noexcept
            }
        }

        public bool TryEvaluate(out float result, object? args = null)
        {
            result = 0f;

            try {
                var calc = expression.GetExpression(args);
                result = Convert.ToSingle(calc.Evaluate(), CultureInfo.InvariantCulture);
                return true;
            } catch (Exception ex) {
                CwlMod.Warn<Cal>("cwl_error_failure".Loc($"{expression}\n{ex.Message}"));
                return false;
                // noexcept
            }
        }

        public Expression GetExpression(object? args = null)
        {
            var tokens = args.Tokenize();
            var calc = Cal.Instance.SymbolicateExpression(expression, tokens.Keys.ToArray());

            foreach (var (k, v) in tokens) {
                calc.SetVariable(k, double.Parse(v));
            }

            return calc;
        }

        public float ArithmeticModOrSet(float lhs)
        {
            return expression.Trim() switch {
                ['+', .. { } raw] when float.TryParse(raw, out var rhs) => lhs + rhs,
                ['+', '+'] => lhs + 1f,
                ['-', .. { } raw] when float.TryParse(raw, out var rhs) => lhs - rhs,
                ['-', '-'] => lhs - 1f,
                ['*', .. { } raw] when float.TryParse(raw, out var rhs) => lhs * rhs,
                ['x', .. { } raw] when float.TryParse(raw, out var rhs) => lhs * rhs,
                ['/', .. { } raw] when float.TryParse(raw, out var rhs) => lhs / rhs,
                ['=', .. { } raw] when float.TryParse(raw, out var rhs) => rhs,
                ['=', '=', .. { } raw] when float.TryParse(raw, out var rhs) => rhs,
                { } raw when float.TryParse(raw, out var rhs) => rhs,
                _ => lhs,
            };
        }

        public int ArithmeticModOrSet(int lhs)
        {
            return expression.Trim() switch {
                ['+', .. { } raw] when int.TryParse(raw, out var rhs) => lhs + rhs,
                ['+', '+'] => lhs + 1,
                ['-', .. { } raw] when int.TryParse(raw, out var rhs) => lhs - rhs,
                ['-', '-'] => lhs - 1,
                ['*', .. { } raw] when int.TryParse(raw, out var rhs) => lhs * rhs,
                ['x', .. { } raw] when int.TryParse(raw, out var rhs) => lhs * rhs,
                ['/', .. { } raw] when int.TryParse(raw, out var rhs) => lhs / rhs,
                ['=', .. { } raw] when int.TryParse(raw, out var rhs) => rhs,
                ['=', '=', .. { } raw] when int.TryParse(raw, out var rhs) => rhs,
                { } raw when int.TryParse(raw, out var rhs) => rhs,
                _ => lhs,
            };
        }

        public float ArithmeticDiff(float lhs)
        {
            return expression.ArithmeticModOrSet(lhs) - lhs;
        }

        public int ArithmeticDiff(int lhs)
        {
            return expression.ArithmeticModOrSet(lhs) - lhs;
        }

        public bool Compare(float lhs)
        {
            return expression.Trim() switch {
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

        public bool Compare(int lhs)
        {
            return expression.Trim() switch {
                ['>', .. { } raw] when int.TryParse(raw, out var rhs) => lhs > rhs,
                ['>', '=', .. { } raw] when int.TryParse(raw, out var rhs) => lhs >= rhs,
                ['=', .. { } raw] when int.TryParse(raw, out var rhs) => lhs == rhs,
                ['=', '=', .. { } raw] when int.TryParse(raw, out var rhs) => lhs == rhs,
                ['!', '=', .. { } raw] when int.TryParse(raw, out var rhs) => lhs != rhs,
                ['<', '=', .. { } raw] when int.TryParse(raw, out var rhs) => lhs <= rhs,
                ['<', .. { } raw] when int.TryParse(raw, out var rhs) => lhs < rhs,
                _ => false,
            };
        }
    }
}