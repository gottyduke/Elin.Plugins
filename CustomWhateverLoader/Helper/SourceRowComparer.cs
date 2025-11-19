using System.Collections.Generic;

namespace Cwl.Helper;

public class SourceRowComparer : IEqualityComparer<SourceData.BaseRow>
{
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