using System.Collections.Generic;
using System.Reflection;

namespace Cwl.Helper.Runtime;

public class OverrideMethodComparer : IEqualityComparer<MethodInfo>
{
    public bool Equals(MethodInfo lhs, MethodInfo rhs)
    {
        return lhs.MetadataToken == rhs.MetadataToken;
    }

    public int GetHashCode(MethodInfo mi)
    {
        return mi.MetadataToken;
    }
}