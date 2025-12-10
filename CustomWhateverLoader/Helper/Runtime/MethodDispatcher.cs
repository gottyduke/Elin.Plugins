using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Helper;

public static class MethodDispatcher
{
    private static readonly HashSet<string> _queried = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, MethodInfo> _cached = new(StringComparer.Ordinal);

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

    extension<T>(T instance)
    {
        public DispatchResult InstanceDispatch(string methodName, params object[] args)
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

        private bool TryGetMethod(string methodName, [NotNullWhen(true)] out MethodInfo? method)
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

            if (_cached.TryGetValue(cacheKey, out method)) {
                return true;
            }

            var allm = instance.GetType().GetMethods(AccessTools.all & ~BindingFlags.Static);
            method = instance.GetType().GetMethod(methodName, AccessTools.all & ~BindingFlags.Static);
            if (method is null) {
                return false;
            }

            _cached[cacheKey] = method;
            return true;
        }
    }

    public sealed record DispatchResult(bool Invoked)
    {
        public Exception? Exception;
        public object? Result;
    }
}