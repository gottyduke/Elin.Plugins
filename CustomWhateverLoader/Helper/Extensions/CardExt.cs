using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class CardExt
{
    extension(Card owner)
    {
        public int GetFlagValue(string flag)
        {
            var key = flag.GetHashCode();
            return owner.mapInt.GetValueOrDefault(key, 0);
        }

        public void SetFlagValue(string flag, int value = 1)
        {
            var key = flag.GetHashCode();
            owner.mapInt[key] = value;
        }
    }
}