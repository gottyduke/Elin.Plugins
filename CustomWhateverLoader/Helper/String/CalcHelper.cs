using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.LangMod;

namespace Cwl.Helper.String;

public static class CalcHelper
{
    public static bool TryEvaluate<T>(this string expression, out T result, object? args = null) where T : unmanaged
    {
        result = default;

        try {
            var tokens = Tokenize(args);
            var calc = Cal.Instance.SymbolicateExpression(expression, tokens.Keys.ToArray());

            foreach (var (k, v) in tokens) {
                calc.SetVariable(k, double.Parse(v));
            }

            result = (T)Convert.ChangeType(calc.Evaluate(), typeof(T));
            return true;
        } catch (Exception ex) {
            CwlMod.Warn<Cal>("cwl_error_failure".Loc(ex.Message));
            return false;
            // noexcept
        }
    }

    public static Dictionary<string, string> Tokenize(object? args)
    {
        if (args is null) {
            return [];
        }

        Dictionary<string, object?> tokens = [];
        if (args is IDictionary<string, object?> input) {
            foreach (var (k, v) in input) {
                tokens[k] = v;
            }
        } else {
            foreach (var property in args.GetType().GetProperties()) {
                tokens[property.Name] = property.GetValue(args);
            }

            foreach (var field in args.GetType().GetFields()) {
                tokens[field.Name] = field.GetValue(args);
            }
        }

        foreach (var (k, v) in tokens.ToArray()) {
            if (v is null) {
                tokens.Remove(k);
            }
        }

        return tokens.ToDictionary(k => k.Key, v => v.Value!.ToString());
    }
}