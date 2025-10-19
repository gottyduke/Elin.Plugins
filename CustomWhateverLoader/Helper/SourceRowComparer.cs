using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cwl.Helper;

public class SourceRowComparer : IEqualityComparer<SourceData.BaseRow>
{
    [field: AllowNull]
    public static SourceRowComparer Default => field ??= new();

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
        return row.GetFieldValue("id")?.GetHashCode() ?? 0;
    }
}