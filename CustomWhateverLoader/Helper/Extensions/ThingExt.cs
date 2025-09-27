using UnityEngine;

namespace Cwl.Helper.Extensions;

public static class ThingExt
{
    extension(Thing thing)
    {
        public void ApplyRangedSocket(string alias)
        {
            var lvBonus = 3 + Mathf.Min(thing.genLv / 10, 15);
            if (!EMono.sources.elements.alias.TryGetValue(alias, out var enchant)) {
                CwlMod.Warn<Thing>($"can't add socket({alias}) to gun {thing.id} because it doesn't exist");
                return;
            }

            var scaler = Mathf.Sqrt(thing.genLv * enchant.encFactor / 100f);
            var totalBonus = lvBonus + scaler;
            var mtp = (enchant.mtp + EClass.rnd(enchant.mtp + (int)totalBonus)) / enchant.mtp;
            if (enchant.encFactor == 0 && mtp > 25) {
                mtp = 25;
            }

            thing.ApplySocket(enchant.id, mtp);
        }
    }
}