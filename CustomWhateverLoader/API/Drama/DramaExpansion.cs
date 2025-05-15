using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.Helper.Runtime;
using Cwl.Helper.Runtime.Exceptions;
using ReflexCLI.Attributes;

// ReSharper disable InconsistentNaming

namespace Cwl.API.Drama;

[ConsoleCommandClassCustomizer("cwl.dm")]
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

    // nodiscard
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

    // nodiscard
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool and(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.All(expr => BuildExpression(expr)!(dm, line));
    }

    // nodiscard
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool or(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.Any(expr => BuildExpression(expr)!(dm, line));
    }

    // nodiscard
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool not(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.All(expr => !BuildExpression(expr)!(dm, line));
    }

    public static bool debug_invoke(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        pc.Say($"debug_invoke : {dm.tg.Name}");

        return true;
    }
}