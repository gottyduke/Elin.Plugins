using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Scripting;

namespace Cwl.API.Drama;

// ReSharper disable InconsistentNaming
public partial class DramaExpansion
{
    /// <summary>
    ///     add_temp_talk(topic)
    /// </summary>
    public static bool add_temp_talk(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var topic);
        dm.RequiresActor(out _);

        AddTempTalk(topic, line["actor"], line["jump"]);

        return true;
    }

    /// <summary>
    ///     build_ext(assembly_name)
    /// </summary>
    public static bool build_ext(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var assemblyName);

        if (assemblyName != "Elin" && !CwlConfig.ExpandedActionsExternal) {
            throw new InvalidOperationException($"{CwlConfig.Dialog.ExpandedActionsAllowExternal!.Definition.Key} is disabled");
        }

        BuildActionList(assemblyName);

        return true;
    }

    /// <summary>
    ///     choice(cmd arg1 arg2 arg3)
    /// </summary>
    [CwlNodiscard]
    public static bool choice(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        var func = BuildExpression(expr);
        return func is not null && func(dm, line);
    }

    /// <summary>
    ///     console_cmd(cmd arg1 arg2 arg3)
    /// </summary>
    public static bool console_cmd(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        string.Join(" ", parameters).ExecuteAsCommand();

        return true;
    }

    public static bool debug_invoke(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        pc.Say($"debug_invoke : {dm.tg.Name}");

        return true;
    }

    /// <summary>
    ///     emit_call(ext.method_name)
    /// </summary>
    [CwlNodiscard]
    public static bool emit_call(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        if (parameters is not [{ } methodName, .. { } pack]) {
            return false;
        }

        if (!_built.TryGetValue(methodName, out var action) || action is null) {
            return false;
        }

        object? result;
        if (!methodName.StartsWith("ext.")) {
            if (pack.Length != action.ParameterCount) {
                throw new DramaActionArgumentException(action.ParameterCount, pack);
            }

            CwlMod.Debug<DramaExpansion>($"emit call [{methodName}]({string.Join(",", parameters)})");
            result = action.Method.FastInvokeStatic(dm, line, pack);
        } else {
            var packs = action.Method.GetParameters()
                .Select(p => Activator.CreateInstance(p.ParameterType))
                .ToArray();
            result = action.Method.FastInvokeStatic(packs);
        }

        return result is true;
    }

    /// <summary>
    ///     eval(csharp)
    /// </summary>
    [CwlNodiscard]
    public static bool eval(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var expr);
        if (expr.IsEmptyOrNull) {
            return false;
        }

        return expr.ExecuteAsCs(new { dm }, DramaScriptState) is true;
    }

    [CwlNodiscard]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool and(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.All(expr => BuildExpression(expr)!(dm, line));
    }

    [CwlNodiscard]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool or(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.Any(expr => BuildExpression(expr)!(dm, line));
    }

    [CwlNodiscard]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool not(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        // can throw
        return parameters.All(expr => !BuildExpression(expr)!(dm, line));
    }
}