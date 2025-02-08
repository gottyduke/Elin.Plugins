using System.Collections.Generic;
using Cwl.Helper.Runtime;

namespace Cwl.Helper;

public struct SourceRowComparer : IEqualityComparer<SourceData.BaseRow>
{
    public static SourceRowComparer Default { get; } = new();

    public bool Equals(SourceData.BaseRow? lhs, SourceData.BaseRow? rhs)
    {
        if (ReferenceEquals(lhs, rhs)) {
            return true;
        }

        if (lhs is null || rhs is null) {
            return false;
        }

        return lhs.GetType() == rhs.GetType() && Equals(lhs.GetFieldValue("id"), rhs.GetFieldValue("id"));
    }

    public int GetHashCode(SourceData.BaseRow row)
    {
        return row.GetType().GetCachedField("id")?.GetValue(row)?.GetHashCode() ?? 0;
    }
}