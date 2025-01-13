using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.ThirdParty;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public static class CachedMethods
{
    private static readonly Dictionary<TypeInfo, MethodWrapper[]> _cached = [];

    public static MethodInfo[] GetCachedMethods(this TypeInfo type)
    {
        if (_cached.TryGetValue(type, out var methods)) {
            return methods.Select(mi => mi.Method).ToArray();
        }

        methods = type.GetMethods(AccessTools.all)
            .Select(mi => new MethodWrapper(mi, mi.GetParameters().Length, type))
            .ToArray();
        _cached[type] = methods;

        return methods.Select(mi => mi.Method).ToArray();
    }

    public static IEnumerable<T> OfDerived<T>(this IEnumerable<T> source, Type baseType) where T : Type
    {
        return source.Where(baseType.IsAssignableFrom);
    }

    [SwallowExceptions]
    public static object? FastInvoke(this MethodInfo method, object? instance, params object[] args)
    {
        return EfficientInvoker.ForMethod(method).Invoke(instance, args);
    }

    [SwallowExceptions]
    public static object? FastInvokeStatic(this MethodInfo method, params object[] args)
    {
        return EfficientInvoker.ForMethod(method).Invoke(null, args);
    }

    internal record MethodWrapper(MethodInfo Method, int ParameterCount, Type Owner);
}