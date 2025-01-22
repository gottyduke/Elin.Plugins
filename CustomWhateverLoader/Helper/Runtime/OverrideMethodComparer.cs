using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cwl.Helper.Runtime;

public struct OverrideMethodComparer : IEqualityComparer<MethodInfo>
{
    public static OverrideMethodComparer Default { get; } = new();

    public bool Equals(MethodInfo lhs, MethodInfo rhs)
    {
        return lhs.MetadataToken == rhs.MetadataToken;
    }

    public int GetHashCode(MethodInfo mi)
    {
        return mi.MetadataToken;
    }

    public static IEnumerable<MethodInfo> FindAllOverrides(Type type, string methodName, params Type[] parameterTypes)
    {
        return type.Assembly.GetTypes()
            .Concat(TypeQualifier.Declared)
            .OfDerived(type)
            .Select(t => t.GetRuntimeMethod(methodName, parameterTypes))
            .Distinct(Default);
    }
}