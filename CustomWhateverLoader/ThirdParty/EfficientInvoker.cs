using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cwl.ThirdParty;

/// <summary>
///     https://github.com/tdupont750/tact.net/blob/master/framework/src/Tact/Reflection/EfficientInvoker.cs
/// </summary>
internal class EfficientInvoker(Func<object?, object[], object?> func)
{
    private static readonly ConcurrentDictionary<ConstructorInfo, Func<object[], object>> _constructorToWrapperMap = [];
    private static readonly ConcurrentDictionary<Type, EfficientInvoker> _typeToWrapperMap = [];

    private static readonly ConcurrentDictionary<MethodKey, EfficientInvoker> _methodToWrapperMap =
        new(MethodKeyComparer.Instance);

    public static Func<object[], object> ForConstructor(ConstructorInfo constructor)
    {
        if (constructor is null) {
            throw new ArgumentNullException(nameof(constructor));
        }

        return _constructorToWrapperMap.GetOrAdd(constructor, _ => {
            CreateParamsExpressions(constructor, out var argsExp, out var paramsExps);

            var newExp = Expression.New(constructor, paramsExps);
            var resultExp = Expression.Convert(newExp, typeof(object));
            return Expression.Lambda<Func<object[], object>>(resultExp, argsExp).Compile();
        });
    }

    public static EfficientInvoker ForDelegate(Delegate del)
    {
        if (del is null) {
            throw new ArgumentNullException(nameof(del));
        }

        var type = del.GetType();
        return _typeToWrapperMap.GetOrAdd(type, t => {
            var method = del.GetMethodInfo();
            return new(CreateMethodWrapper(t, method, true));
        });
    }

    public static EfficientInvoker ForMethod(MethodInfo method)
    {
        if (method is null) {
            throw new ArgumentNullException(nameof(method));
        }

        var key = new MethodKey(method.DeclaringType!, method.Name);
        return _methodToWrapperMap.GetOrAdd(key, k => {
            var wrapper = CreateMethodWrapper(k.Type, method, false);
            return new(wrapper);
        });
    }

    public static EfficientInvoker ForProperty(Type type, string propertyName)
    {
        if (type is null) {
            throw new ArgumentNullException(nameof(type));
        }

        if (propertyName is null) {
            throw new ArgumentNullException(nameof(propertyName));
        }

        var key = new MethodKey(type, propertyName);
        return _methodToWrapperMap.GetOrAdd(key, _ => new(CreatePropertyWrapper(type, propertyName)));
    }

    public object? Invoke(object? instance, params object[] args)
    {
        return func(instance, args);
    }

    public static Func<object?, object[], object?> CreateMethodWrapper(Type type, MethodInfo method, bool isDelegate)
    {
        CreateParamsExpressions(method, out var argsExp, out var paramsExps);

        var thisExp = Expression.Parameter(typeof(object), "this");
        var castTargetExp = Expression.Convert(thisExp, type);
        Expression invokeExp = isDelegate
            ? Expression.Invoke(castTargetExp, paramsExps)
            : method.IsStatic
                ? Expression.Call(method, paramsExps)
                : Expression.Call(castTargetExp, method, paramsExps);

        LambdaExpression lambdaExp;

        if (method.ReturnType != typeof(void)) {
            var resultExp = Expression.Convert(invokeExp, typeof(object));
            lambdaExp = Expression.Lambda(resultExp, thisExp, argsExp);
        } else {
            var constExp = Expression.Constant(null, typeof(object));
            var blockExp = Expression.Block(invokeExp, constExp);
            lambdaExp = Expression.Lambda(blockExp, thisExp, argsExp);
        }

        return (Func<object?, object[], object?>)lambdaExp.Compile();
    }

    private static void CreateParamsExpressions(MethodBase method, out ParameterExpression paramsExp,
        out Expression[] paramsExps)
    {
        var parameters = method.GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        paramsExp = Expression.Parameter(typeof(object[]), "params");
        paramsExps = new Expression[parameters.Count];

        for (var i = 0; i < parameters.Count; ++i) {
            var constExp = Expression.Constant(i, typeof(int));
            paramsExps[i] = Expression.Convert(Expression.ArrayIndex(paramsExp, constExp), parameters[i]);
        }
    }

    private static Func<object?, object[], object?> CreatePropertyWrapper(Type type, string propertyName)
    {
        var property = type.GetRuntimeProperty(propertyName);
        var thisExp = Expression.Parameter(typeof(object), "this");
        var paramsExp = Expression.Parameter(typeof(object[]), "params");
        var castArgExp = Expression.Convert(thisExp, type);
        var propExp = Expression.Property(castArgExp, property);
        var castPropExp = Expression.Convert(propExp, typeof(object));
        return Expression.Lambda<Func<object?, object[], object?>>(castPropExp, thisExp, paramsExp).Compile();
    }

    private class MethodKeyComparer : IEqualityComparer<MethodKey>
    {
        public static readonly MethodKeyComparer Instance = new();

        public bool Equals(MethodKey lhs, MethodKey rhs)
        {
            return lhs.Type == rhs.Type && StringComparer.Ordinal.Equals(lhs.Name, rhs.Name);
        }

        public int GetHashCode(MethodKey key)
        {
            var typeCode = key.Type.GetHashCode();
            var methodCode = key.Name.GetHashCode();
            return CombineHashCodes(typeCode, methodCode);
        }

        // From System.Web.Util.HashCodeCombiner
        private static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }
    }

    private record MethodKey(Type Type, string Name);
}