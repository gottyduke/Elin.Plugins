using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper;

public static class MethodDispatcher
{
    private static readonly Dictionary<string, HashSet<MethodInfo>> _cached = [];

    public static void InstanceDispatch<T>(this T instance, string methodName, params object[] args)
    {
        if (instance is null) {
            return;
        }

        var cache = $"{instance.GetType().FullName}::{methodName}";
        if (!_cached.TryGetValue(cache, out var cachedMethods) ||
            cachedMethods.Count == 0) {
            return;
        }

        foreach (var method in cachedMethods) {
            try {
                Array.Resize(ref args, method.GetParameters().Length);
                method.Invoke(instance, args);
            } catch {
                // noexcept
            }
        }
    }

    internal static void BuildDispatchList<T>(string methodName)
    {
        foreach (var type in TypeQualifier.Declared.Where(t => typeof(T).IsAssignableFrom(t))) {
            try {
                var cache = $"{type.FullName}::{methodName}";
                _cached.TryAdd(cache, []);

                var invocable = AccessTools.GetDeclaredMethods(type).FirstOrDefault(mi => mi.Name == methodName);
                if (invocable is not null) {
                    _cached[cache].Add(invocable);
                }
            } catch {
                // noexcept
            }
        }
    }
}