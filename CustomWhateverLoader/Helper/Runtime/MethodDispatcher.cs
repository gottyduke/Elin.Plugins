using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.Extensions;

namespace Cwl.Helper;

public static class MethodDispatcher
{
    private static readonly HashSet<string> _queried = [];
    private static readonly Dictionary<string, MethodInfo> _cached = [];

    public static DispatchResult InstanceDispatch<T>(this T instance, string methodName, params object[] args)
    {
        if (instance is null ||
            !TryGetMethod(instance, methodName, out var method) ||
            !method.ValidateParameters(args)) {
            return new(false);
        }

        var dispatch = new DispatchResult(true);
        try {
            dispatch.Result = method.FastInvoke(instance, args);
        } catch (Exception ex) {
            dispatch.Exception = ex;
            CwlMod.Warn($"Invocation failed: {ex}");
        }

        return dispatch;
    }

    private static bool TryGetMethod<T>(T instance, string methodName, out MethodInfo method)
    {
        var cacheKey = $"{instance!.GetType().FullName!}::{methodName}";
        if (_cached.TryGetValue(cacheKey, out method)) {
            return true;
        }

        if (_queried.Contains(cacheKey)) {
            return false;
        }

        BuildDispatchList<T>(methodName);
        _queried.Add(cacheKey);

        return _cached.TryGetValue(cacheKey, out method);
    }

    [SwallowExceptions]
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