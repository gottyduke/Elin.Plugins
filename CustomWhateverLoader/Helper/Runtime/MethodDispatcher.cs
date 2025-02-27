using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public static class MethodDispatcher
{
    private static readonly HashSet<string> _queried = [];
    private static readonly Dictionary<string, MethodInfo> _cached = [];

    public static DispatchResult InstanceDispatch<T>(this T instance, string methodName, params object[] args)
    {
        if (instance is null) {
            return new(false);
        }

        var cache = $"{instance.GetType().FullName}::{methodName}";
        if (!_cached.ContainsKey(cache) && !_queried.Contains(cache)) {
            // build from instance runtime type info
            BuildDispatchList<T>(methodName);
            _queried.Add(cache);
        }

        if (!_cached.TryGetValue(cache, out var method)) {
            return new(false);
        }

        if (!method.GetParameters().Types().SequenceEqual(args.Select(a => a.GetType()))) {
            CwlMod.Warn($"failed invoking {method.Name}\nunexpected parameters pack");
            return new(false);
        }

        var dispatch = new DispatchResult(true);
        try {
            dispatch.Result = method.FastInvoke(instance, args);
        } catch (Exception ex) {
            dispatch.Exception = ex;
            CwlMod.Warn($"failed invoking {method.Name}\n{ex}");
            // noexcept
        }

        return dispatch;
    }

    internal static void BuildDispatchList<T>(string methodName)
    {
        foreach (var type in TypeQualifier.Declared.OfDerived(typeof(T))) {
            try {
                var invocable = Array.Find(type.GetCachedMethods(), mi => mi.Name == methodName);
                if (invocable is null) {
                    continue;
                }

                var cache = $"{type.FullName}::{methodName}";
                _cached[cache] = invocable;
            } catch {
                // noexcept
            }
        }
    }

    public sealed record DispatchResult(bool Invoked)
    {
        public Exception? Exception;
        public object? Result;
    }
}