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

    public static MethodInfo[] GetCachedMethods(this Type type)
    {
        return GetCachedMethods(type.GetTypeInfo());
    }

    public static MethodInfo[] GetCachedMethods(this TypeInfo type)
    {
        if (_cached.TryGetValue(type, out var methods)) {
            return methods;
        }

        return _cached[type] = AccessTools.GetDeclaredMethods(type).ToArray();
    }

    public static MethodInfo? GetCachedMethod(string typeName, string methodName, Type[] parameters)
    {
        try {
            var type = TryGetType(typeName);
            if (type is null) {
                return null;
            }

            return Array.Find(type.GetCachedMethods(),
                mi => mi.Name == methodName &&
                      mi.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameters));
        } catch {
            return null;
            // noexcept
        }
    }

    public static Type? TryGetType(string typeName)
    {
        try {
            return AccessTools.TypeByName(typeName);
        } catch {
            return null;
            // noexcept
        }
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