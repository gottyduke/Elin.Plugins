using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public static class CachedMethods
{
    private static readonly Dictionary<TypeInfo, MethodInfo[]> _cached = [];

    public static IEnumerable<MethodInfo> GetCachedMethods(this TypeInfo type)
    {
        if (_cached.TryGetValue(type, out var methods)) {
            return methods;
        }

        methods = type.GetMethods(AccessTools.all);
        _cached[type] = methods;

        return methods;
    }

    public static IEnumerable<T> OfDerived<T>(this IEnumerable<T> source, Type baseType) where T : Type
    {
        return source.Where(baseType.IsAssignableFrom);
    }
}