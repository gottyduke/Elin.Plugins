using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl.API.Drama;

/// <summary>
///     CWL loads the static methods from derived <see cref="DramaOutcome" /> classes but it will not be instantiated
/// </summary>
public partial class DramaExpansion
{
    private static readonly Dictionary<string, ActionWrapper?> _built = [];
    private static readonly Dictionary<string, Func<DramaManager, Dictionary<string, string>, bool>> _expressions = [];

    public static IReadOnlyDictionary<string, MethodInfo> Actions => _built.ToDictionary(kv => kv.Key, kv => kv.Value!.Method);

    [Time]
    [SwallowExceptions]
    [ConsoleCommand("build_action_list")]
    internal static string BuildActionList(string assemblyName = "")
    {
        IEnumerable<MethodInfo> methods;

        var external = assemblyName != "";
        if (external) {
            if (!_built.TryAdd($"ext.cwl_stub_{assemblyName}", null)) {
                return "";
            }

            var asm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == assemblyName);
            if (asm is null) {
                return "";
            }

            CwlMod.Log<DramaExpansion>("cwl_log_drama_build_ext".Loc(assemblyName));

            methods = AccessTools.GetTypesFromAssembly(asm)
                .SelectMany(AccessTools.GetDeclaredMethods)
                .Where(mi => mi is { IsStatic: true, IsGenericMethod: false, IsSpecialName: false });
        } else {
            methods = TypeQualifier.Declared.OfDerived(typeof(DramaOutcome))
                .SelectMany(AccessTools.GetDeclaredMethods)
                .Where(mi => mi is { IsStatic: true, IsGenericMethod: false, IsSpecialName: false })
                .Where(mi => Delegate.CreateDelegate(typeof(DramaAction), mi, false) is not null)
                .Concat(AttributeQuery.MethodsWith<CwlDramaExpansion>().Select(aq => aq.Item1));
        }

        var count = 0;
        foreach (var method in methods) {
            var type = method.DeclaringType!;
            var entry = external ? $"ext.{type.Name}.{method.Name}" : method.Name;
            _built[entry] = new(method, method.GetParameters().Length, type);
            count++;
        }

        var log = "cwl_log_drama_build".Loc(count);
        CwlMod.Log<DramaExpansion>(log);

        return log;
    }

    [ConsoleCommand("dump_action_list")]
    internal static string DumpActionList()
    {
        using var sb = StringBuilderPool.Get();
        sb.AppendLine("dumping action list");

        foreach (var (action, provider) in Actions) {
            sb.AppendLine($"{action,-30}{provider.GetAssemblyDetailParams(false)}");
        }

        var log = sb.ToString();
        CwlMod.Log<DramaExpansion>(log);

        return log;
    }

    internal static Func<DramaManager, Dictionary<string, string>, bool>? BuildExpression(string? expression)
    {
        if (expression is null) {
            return null;
        }

        if (_expressions.TryGetValue(expression, out var cached)) {
            return cached;
        }

        var parse = Regex.Match(expression, @"^\s*(?<func>\w+)\s*\(\s*(?<params>.*)\s*\)\s*$");
        if (!parse.Success) {
            return null;
        }

        var funcName = parse.Groups["func"].Value;
        if (!_built.TryGetValue(funcName, out var func) || func is null) {
            return null;
        }

        var parameters = parse.Groups["params"].Value.IsEmpty("");
        var pack = funcName switch {
            nameof(and) or nameof(or) or nameof(not) => Regex.Matches(parameters, @"\w+\(.*?\)").Select(m => m.Value),
            _ => SplitParams(parameters),
        };

        return _expressions[expression] = (dm, line) => SafeInvoke(func, dm, line, pack.ToArray());
    }

    private static IEnumerable<string> SplitParams(string parameters)
    {
        var matches = Regex.Matches(parameters, """(\"(?<args>.*?)\"|(?<args>[^,]+))""");
        foreach (Match m in matches) {
            var content = m.Groups["args"].Value.Trim();
            yield return content;
        }
    }

    private delegate bool DramaAction(DramaManager dm, Dictionary<string, string> line, params string[] parameters);

    internal record ActionWrapper(MethodInfo Method, int ParameterCount, Type Owner);
}