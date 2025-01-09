﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cwl.Helper.Runtime;

public static class MethodDispatcher
{
    private static readonly HashSet<string> _queried = [];
    private static readonly Dictionary<string, HashSet<MethodInfo>> _cached = [];

    public static void InstanceDispatch<T>(this T instance, string methodName, params object[] args)
    {
        if (instance is null) {
            return;
        }

        var cache = $"{instance.GetType().FullName}::{methodName}";
        if (!_cached.TryGetValue(cache, out var cachedMethods)) {
            if (!_queried.Contains(cache)) {
                // build from instance runtime type info
                BuildDispatchList<T>(methodName);
                _queried.Add(cache);
            }

            if (cachedMethods?.Count is not > 0) {
                return;
            }
        }

        foreach (var method in cachedMethods) {
            method.SafeInvoke(instance, args);
        }
    }

    [SwallowExceptions]
    public static object? SafeInvoke(this MethodInfo method, object instance, params object[] args)
    {
        Array.Resize(ref args, method.GetParameters().Length);
        return method.Invoke(instance, args);
    }

    internal static void BuildDispatchList<T>(string methodName)
    {
        foreach (var type in TypeQualifier.Declared.OfDerived(typeof(T))) {
            try {
                var cache = $"{type.FullName}::{methodName}";
                _cached.TryAdd(cache, []);

                var invocable = type.GetCachedMethods().FirstOrDefault(mi => mi.Name == methodName);
                if (invocable is not null) {
                    _cached[cache].Add(invocable);
                }
            } catch {
                // noexcept
            }
        }
    }
}