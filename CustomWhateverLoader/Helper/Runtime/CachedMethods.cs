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
    private static readonly Dictionary<int, FastInvokeHandler> _cachedInvokers = [];

    public static MethodInfo? GetCachedMethod(string typeName, string methodName, Type[] parameters)
    {
        try {
            var type = TypeQualifier.GlobalResolve(typeName);
            if (type?.IsGenericType is not false) {
                return null;
            }

            return Array.Find(type.GetCachedMethods(),
                mi => mi.Name == methodName &&
                      mi.ValidateParameterTypes(false, parameters));
        } catch {
            return null;
            // noexcept
        }
    }

    public static object? GetFieldValue(this object instance, string fieldName)
    {
        return instance.GetType().GetCachedField(fieldName)?.GetValue(instance);
    }

    extension(Type type)
    {
        public MethodInfo[] GetCachedMethods()
        {
            return GetCachedMethods(type.GetTypeInfo());
        }

        public FieldInfo[] GetCachedFields()
        {
            return GetCachedFields(type.GetTypeInfo());
        }

        public FieldInfo? GetCachedField(string fieldName)
        {
            return GetCachedField(type.GetTypeInfo(), fieldName);
        }
    }

    extension(TypeInfo typeInfo)
    {
        public MethodInfo[] GetCachedMethods()
        {
            if (_cachedMethods.TryGetValue(typeInfo, out var methods)) {
                return methods;
            }

            return _cachedMethods[typeInfo] = AccessTools.GetDeclaredMethods(typeInfo).ToArray();
        }

        public FieldInfo[] GetCachedFields()
        {
            if (_cachedFields.TryGetValue(typeInfo, out var fields)) {
                return fields;
            }

            return _cachedFields[typeInfo] = typeInfo.GetFields(AccessTools.all & ~BindingFlags.Static);
        }

        public FieldInfo? GetCachedField(string fieldName)
        {
            return GetCachedFields(typeInfo).FirstOrDefault(f => f.Name == fieldName);
        }
    }

    extension(MethodInfo method)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? FastInvoke(object? instance, params object[] args)
        {
            if (!_cachedInvokers.TryGetValue(method.MetadataToken, out var invoker)) {
                invoker = _cachedInvokers[method.MetadataToken] = MethodInvoker.GetHandler(method, true);
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
            var parameters = method.GetParameters();

            if (parameters.Length != types.Length) {
                if (warn) {
                    CwlMod.Warn($"parameter count mismatch for {method.Name}: expected {parameters.Length}, got {types.Length}");
                }
                return false;
            }

            for (var i = 0; i < parameters.Length; ++i) {
                var paramType = parameters[i].ParameterType;
                var type = types[i];

                if (type == null) {
                    if (!paramType.IsValueType || Nullable.GetUnderlyingType(paramType) != null) {
                        continue;
                    }

                    CwlMod.Warn($"parameter {i} type mismatch: expected {paramType.Name}, got null (non-nullable value type)");
                    return false;
                }

                if (paramType.IsAssignableFrom(type)) {
                    continue;
                }

                CwlMod.Warn($"parameter {i} type mismatch: expected {paramType.Name}, got {type.Name}");
                return false;
            }

            return true;
        }
    }
}