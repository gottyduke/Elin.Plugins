using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;
using MethodTimer;

namespace Cwl.API.Drama;

/// <summary>
///     CWL loads the static methods from derived <see cref="DramaOutcome" /> classes but it will not be instantiated
/// </summary>
public partial class DramaExpansion : DramaOutcome
{
    private static bool _externalBuilt;
    internal static readonly Dictionary<string, CachedMethods.MethodWrapper> Cached = [];

    public static IReadOnlyDictionary<string, MethodInfo> Actions => Cached.ToDictionary(kv => kv.Key, kv => kv.Value.Method);

    [Time]
    internal static void BuildActionList(bool withElin = false)
    {
        IEnumerable<Type> types;

        if (withElin) {
            if (_externalBuilt) {
                return;
            }

            _externalBuilt = true;
            types = (TypeQualifier.Plugins ?? [])
                .Select(p => p.GetType().Assembly)
                // include Elin as well
                .Append(typeof(DramaOutcome).Assembly)
                // with no type restriction
                .SelectMany(p => p.GetTypes());
        } else {
            types = TypeQualifier.Declared
                .OfDerived(typeof(DramaOutcome));
        }

        var methods = types
            .SelectMany(AccessTools.GetDeclaredMethods)
            .Where(mi => mi is { IsStatic: true, IsGenericMethod: false, IsSpecialName: false })
            .Where(mi => Delegate.CreateDelegate(typeof(DramaAction), mi, false) is not null);
        foreach (var method in methods) {
            Cached[method.Name] = new(method, method.GetParameters().Length, method.DeclaringType!);
        }

        CwlMod.Log<DramaExpansion>($"rebuilt {Cached.Count} methods");
    }
}