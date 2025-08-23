using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class CardExt
{
    extension(Card owner)
    {
        public int GetFlagValue(string flag)
        {
            var key = flag.GetHashCode();
            if (owner.mapInt.TryGetValue(key, out var value)) {
                return value;
            }

            if (owner.IsPC) {
                value = EClass.player.dialogFlags.GetValueOrDefault(flag, 0);
            }

            return value;
        }

        public void SetFlagValue(string flag, int value = 1)
        {
            var key = flag.GetHashCode();
            owner.mapInt[key] = value;

            if (owner.IsPC) {
                EClass.player.dialogFlags[flag] = value;
            }
        }
    }
}