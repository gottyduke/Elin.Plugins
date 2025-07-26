using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Cwl.Helper;

public static class CachedMethods
{
    private static readonly Dictionary<TypeInfo, MethodInfo[]> _cachedMethods = [];
    private static readonly Dictionary<TypeInfo, FieldInfo[]> _cachedFields = [];
    private static readonly Dictionary<MethodInfo, FastInvokeHandler> _cachedInvokers = [];

    public static MethodInfo[] GetCachedMethods(this Type type)
    {
        return GetCachedMethods(type.GetTypeInfo());
    }

    public static MethodInfo[] GetCachedMethods(this TypeInfo type)
    {
        if (_cachedMethods.TryGetValue(type, out var methods)) {
            return methods;
        }

        return _cachedMethods[type] = AccessTools.GetDeclaredMethods(type).ToArray();
    }

    public static MethodInfo? GetCachedMethod(string typeName, string methodName, Type[] parameters)
    {
        try {
            var type = TypeQualifier.GlobalResolve(typeName);
            if (type?.IsGenericType is not false) {
                return null;
            }

            return Array.Find(type.GetCachedMethods(),
                mi => mi.Name == methodName &&
                      mi.GetParameters().Types().SequenceEqual(parameters));
        } catch {
            return null;
            // noexcept
        }
    }

    public static FieldInfo[] GetCachedFields(this Type type)
    {
        return GetCachedFields(type.GetTypeInfo());
    }

    public static FieldInfo[] GetCachedFields(this TypeInfo type)
    {
        if (_cachedFields.TryGetValue(type, out var fields)) {
            return fields;
        }

        return _cachedFields[type] = type.GetFields(AccessTools.all & ~BindingFlags.Static);
    }
    public static FieldInfo? GetCachedField(this Type type, string fieldName)
    {
        return GetCachedField(type.GetTypeInfo(), fieldName);
    }

    public static FieldInfo? GetCachedField(this TypeInfo type, string fieldName)
    {
        return GetCachedFields(type).FirstOrDefault(f => f.Name == fieldName);
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