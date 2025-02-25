using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public static class CachedMethods
{
    private static readonly Dictionary<TypeInfo, MethodInfo[]> _cached = [];
    private static readonly Dictionary<TypeInfo, FieldInfo?> _cachedFields = [];
    private static readonly Dictionary<MethodInfo, FastInvokeHandler> _cachedInvokers = [];

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
            var type = TypeQualifier.GlobalResolve(typeName);
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

    public static FieldInfo? GetCachedField(this Type type, string fieldName)
    {
        return GetCachedField(type.GetTypeInfo(), fieldName);
    }

    public static FieldInfo? GetCachedField(this TypeInfo type, string fieldName)
    {
        if (_cachedFields.TryGetValue(type, out var field)) {
            return field;
        }

        return _cachedFields[type] = AccessTools.Field(type, fieldName);
    }

    public static object? GetFieldValue(this object instance, string fieldName)
    {
        return instance.GetType().GetCachedField(fieldName)?.GetValue(instance);
    }

    public static IEnumerable<T> OfDerived<T>(this IEnumerable<T> source, Type baseType) where T : Type
    {
        return source.Where(baseType.IsAssignableFrom);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? FastInvoke(this MethodInfo method, object? instance, params object[] args)
    {
        if (!_cachedInvokers.TryGetValue(method, out var invoker)) {
            invoker = _cachedInvokers[method] = MethodInvoker.GetHandler(method, true);
        }

        return invoker.Invoke(instance, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? FastInvokeStatic(this MethodInfo method, params object[] args)
    {
        return method.FastInvoke(null, args);
    }
}