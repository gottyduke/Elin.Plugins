using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cwl.Helper;

public static class AttributeQuery
{
    [SwallowExceptions]
    public static IEnumerable<(Type, T[])> TypesWith<T>(bool inherit = true) where T : Attribute
    {
        foreach (var type in TypeQualifier.Declared) {
            T[] attrs = [];

            try {
                attrs = type.GetCustomAttributes<T>(inherit).ToArray();
            } catch {
                // noexcept
            }

            if (attrs.Length > 0) {
                yield return new(type, attrs);
            }
        }
    }

    public static IEnumerable<(MethodInfo, T[])> MethodsWith<T>(bool inherit = true) where T : Attribute
    {
        foreach (var method in TypeQualifier.Declared.SelectMany(CachedMethods.GetCachedMethods)) {
            T[] attrs = [];

            try {
                attrs = method.GetCustomAttributes<T>(inherit).ToArray();
            } catch {
                // noexcept
            }

            if (attrs.Length > 0) {
                yield return new(method, attrs);
            }
        }
    }
}