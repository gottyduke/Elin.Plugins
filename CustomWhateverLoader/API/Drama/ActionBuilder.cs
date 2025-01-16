using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cwl.Helper.Runtime;
using HarmonyLib;
using MethodTimer;

namespace Cwl.API.Drama;

/// <summary>
///     CWL loads the static methods from derived <see cref="DramaOutcome" /> classes but it will not be instantiated
/// </summary>
public partial class DramaExpansion : DramaOutcome
{
    private static readonly Dictionary<string, ActionWrapper?> _built = [];
    private static readonly Dictionary<string, Func<DramaManager, Dictionary<string, string>, bool>> _expressions = [];

    public static IReadOnlyDictionary<string, MethodInfo> Actions => _built.ToDictionary(kv => kv.Key, kv => kv.Value!.Method);

    [Time]
    internal static void BuildActionList(string assemblyName = "")
    {
        IEnumerable<MethodInfo> methods;

        var external = assemblyName != "";
        if (external) {
            if (!_built.TryAdd($"ext.cwl_stub_{assemblyName}", null)) {
                return;
            }

            var asm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == assemblyName);
            if (asm is null) {
                return;
            }

            CwlMod.Log<DramaExpansion>($"building external methods from {assemblyName}");

            methods = AccessTools.GetTypesFromAssembly(asm)
                .SelectMany(AccessTools.GetDeclaredMethods)
                .Where(mi => mi is { IsStatic: true, IsGenericMethod: false, IsSpecialName: false });
        } else {
            methods = TypeQualifier.Declared.Keys.OfDerived(typeof(DramaOutcome))
                .SelectMany(AccessTools.GetDeclaredMethods)
                .Where(mi => mi is { IsStatic: true, IsGenericMethod: false, IsSpecialName: false })
                .Where(mi => Delegate.CreateDelegate(typeof(DramaAction), mi, false) is not null);
        }

        var count = 0;
        foreach (var method in methods) {
            var entry = external ? $"ext.{method.DeclaringType!.Name}.{method.Name}" : method.Name;
            _built[entry] = new(method, method.GetParameters().Length, method.DeclaringType!);
            count++;
        }

        CwlMod.Log<DramaExpansion>($"rebuilt {count} methods");
    }

    internal static Func<DramaManager, Dictionary<string, string>, bool>? BuildExpression(string? expression)
    {
        if (expression is null) {
            return null;
        }

        if (_expressions.TryGetValue(expression, out var cached)) {
            return cached;
        }

        var parse = Regex.Match(expression.Replace("\"", ""), @"^(?<func>\w+)\((?<params>.*)?\)$");
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
            _ => parameters.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()),
        };

        return _expressions[expression] = (dm, line) => SafeInvoke(func, dm, line, pack.ToArray());
    }

    private delegate bool DramaAction(DramaManager dm, Dictionary<string, string> line, params string[] parameters);

    internal record ActionWrapper(MethodInfo Method, int ParameterCount, Type Owner);
}