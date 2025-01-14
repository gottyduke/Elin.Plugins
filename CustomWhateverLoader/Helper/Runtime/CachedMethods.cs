using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cwl.ThirdParty;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public static class CachedMethods
{
    private static readonly Dictionary<TypeInfo, MethodInfo[]> _cached = [];

    public static MethodInfo[] GetCachedMethods(this TypeInfo type)
    {
        if (_cached.TryGetValue(type, out var methods)) {
            return methods;
        }

        methods = type.GetMethods(AccessTools.all);
        return _cached[type] = methods;
    }

    public static IEnumerable<T> OfDerived<T>(this IEnumerable<T> source, Type baseType) where T : Type
    {
        return source.Where(baseType.IsAssignableFrom);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? FastInvoke(this MethodInfo method, object? instance, params object[] args)
    {
        return EfficientInvoker.ForMethod(method).Invoke(instance, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? FastInvokeStatic(this MethodInfo method, params object[] args)
    {
        return EfficientInvoker.ForMethod(method).Invoke(null, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? FastInvoke(this Delegate del, object? instance, params object[] args)
    {
        return EfficientInvoker.ForDelegate(del).Invoke(instance, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? FastInvokeStatic(this Delegate del, params object[] args)
    {
        return EfficientInvoker.ForDelegate(del).Invoke(null, args);
    }
}