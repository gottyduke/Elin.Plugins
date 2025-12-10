using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Helper;

public static class CachedMethods
{
    private static readonly Dictionary<TypeInfo, MethodInfo[]> _cachedMethods = [];
    private static readonly Dictionary<MethodBase, FastInvokeHandler> _cachedInvokers = [];

    public static MethodInfo? GetCachedMethod(string typeName, string methodName, Type[] types)
    {
        return GetCachedMethod(typeName, methodName, types.Select(t => (t.FullName, (string?)null)).ToArray());
    }

    public static MethodInfo? GetCachedMethod(string typeName, string methodName, IReadOnlyList<(string?, string?)> parameters)
    {
        try {
            var type = TypeQualifier.GlobalResolve(typeName);
            var cachedMethods = type.GetCachedMethods();

            var parameterInfo = string.Join(", ", parameters.Select(p => p.Item1));
            var runtimeInfo = $"{methodName}({parameterInfo})";

            var method = Array.Find(cachedMethods, mi => mi.ToString().EndsWith(runtimeInfo));
            if (method is not null) {
                return method;
            }

            var nonGenericName = methodName.Split('[')[0];
            return Array.Find(cachedMethods, mi => mi.Name == nonGenericName &&
                                                   mi.ValidateParameterTypes(false, parameters));
        } catch {
            return null;
            // noexcept
        }
    }

    extension(Type type)
    {
        public MethodInfo[] GetCachedMethods()
        {
            var typeInfo = type.GetTypeInfo();
            if (_cachedMethods.TryGetValue(typeInfo, out var methods)) {
                return methods;
            }

            return _cachedMethods[typeInfo] = AccessTools.GetDeclaredMethods(typeInfo).ToArray();
        }
    }

    extension(MethodInfo method)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? FastInvoke(object? instance, params object[] args)
        {
            if (!_cachedInvokers.TryGetValue(method, out var invoker)) {
                invoker = _cachedInvokers[method] = MethodInvoker.GetHandler(method, true);
            }

            return invoker.Invoke(instance, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? FastInvokeStatic(params object[] args)
        {
            return method.FastInvoke(null, args);
        }

        public bool ValidateParameters(params object?[] args)
        {
            return method.ValidateParameterTypes(false, args.Select(o => o?.GetType()).ToArray());
        }

        public bool ValidateParameterTypes(bool warn, params Type?[] types)
        {
            return method.ValidateParameterTypes(warn, types.Select(t => (t?.FullName, (string?)null)).ToArray());
        }

        public bool ValidateParameterTypes(bool warn, IReadOnlyList<(string?, string?)> types)
        {
            var parameters = method.GetParameters();

            if (parameters.Length != types.Count) {
                if (warn) {
                    CwlMod.Warn($"{method.Name} parameter count mismatch: expected {parameters.Length}, got {types.Count}");
                }

                return false;
            }

            for (var i = 0; i < parameters.Length; ++i) {
                var parameter = parameters[i];
                var paramType = parameter.ParameterType;
                var (type, name) = types[i];

                if (type.IsEmptyOrNull) {
                    if (!paramType.IsValueType || Nullable.GetUnderlyingType(paramType) != null) {
                        continue;
                    }

                    CwlMod.Warn($"{method.Name} parameter {i} type mismatch: expected {paramType.Name}, got null !!(value type)");
                    return false;
                }

                var concreteType = TypeQualifier.GlobalResolve(type) ?? TypeQualifier.AliasMapping.GetValueOrDefault(type);
                if (paramType.IsAssignableFrom(concreteType)) {
                    continue;
                }

                var paramInfo = paramType.ToString();
                if (paramInfo == type) {
                    continue;
                }

                var nonGenericName = $"{paramType.Namespace}.{paramType.Name}";
                if ((name is null or "" or "null" || name == parameter.Name) &&
                    type.StartsWith(nonGenericName)) {
                    continue;
                }

                CwlMod.Warn($"{method.Name} parameter {i} type mismatch: expected {paramInfo}, got {type}:{name}");
                return false;
            }

            return true;
        }
    }
}