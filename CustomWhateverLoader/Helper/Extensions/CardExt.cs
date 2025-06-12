using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class CardExt
{
    public static int GetFlagValue(this Card owner, string flag)
    {
        var key = flag.GetHashCode();
        return owner.mapInt.GetValueOrDefault(key, 0);
    }

    public static void SetFlagValue(this Card owner, string flag, int value = 1)
    {
        var key = flag.GetHashCode();
        owner.mapInt[key] = value;
    }
}