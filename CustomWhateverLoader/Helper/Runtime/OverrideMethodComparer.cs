using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public struct OverrideMethodComparer : IEqualityComparer<MethodBase>
{
    public static OverrideMethodComparer Default { get; } = new();

    public bool Equals(MethodBase? lhs, MethodBase? rhs)
    {
        return ReferenceEquals(lhs, rhs) || lhs?.MetadataToken == rhs?.MetadataToken;
    }

    public int GetHashCode(MethodBase mi)
    {
        return mi.MetadataToken;
    }

    public static IEnumerable<MethodBase> FindAllOverrides(Type type, string methodName, params Type[] parameterTypes)
    {
        return type.Assembly.GetTypes()
            .Concat(TypeQualifier.Declared)
            .OfDerived(type)
            .Select(t => t.GetRuntimeMethod(methodName, parameterTypes))
            .Distinct(Default);
    }

    public static IEnumerable<MethodBase> FindAllOverridesGetter(Type type, string propertyName)
    {
        return FindAllOverrides(type, $"get_{propertyName}");
    }

    public static IEnumerable<MethodBase> FindAllOverridesCtor(Type type)
    {
        return type.Assembly.GetTypes()
            .Concat(TypeQualifier.Declared)
            .OfDerived(type)
            .SelectMany(t => AccessTools.GetDeclaredConstructors(t))
            .Distinct(Default);
    }
}