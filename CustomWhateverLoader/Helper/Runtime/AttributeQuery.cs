using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cwl.Helper.Runtime;

public class AttributeQuery
{
    [SwallowExceptions]
    public static IEnumerable<Tuple<Type, T[]>> TypesWith<T>(bool inherit = true) where T : Attribute
    {
        foreach (var type in TypeQualifier.Declared) {
            var attrs = type.GetCustomAttributes<T>(inherit).ToArray();
            if (attrs.Length > 0) {
                yield return new(type, attrs);
            }
        }
    }

    public static IEnumerable<Tuple<MethodInfo, T[]>> MethodsWith<T>(bool inherit = true) where T : Attribute
    {
        foreach (var method in TypeQualifier.Declared.SelectMany(CachedMethods.GetCachedMethods)) {
            var attrs = method.GetCustomAttributes<T>(inherit).ToArray();
            if (attrs.Length > 0) {
                yield return new(method, attrs);
            }
        }
    }
}