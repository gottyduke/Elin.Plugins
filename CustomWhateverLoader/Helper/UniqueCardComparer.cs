using System.Collections.Generic;

namespace Cwl.Helper;

public class UniqueCardComparer : IEqualityComparer<Card>
{
    public static UniqueCardComparer Default => field ??= new();

    public bool Equals(Card? lhs, Card? rhs)
    {
        if (ReferenceEquals(lhs, rhs)) {
            return true;
        }

        if (lhs is null || rhs is null) {
            return false;
        }

        return lhs.GetType() == rhs.GetType() && Equals(lhs.id, rhs.id);
    }

    public int GetHashCode(Card card)
    {
        return card.id.GetHashCode();
    }
}