using System.Collections.Generic;
using System.Linq;

namespace Emmersive.Helper;

public static class PointScan
{
    extension(Chara focus)
    {
        public List<Chara> Nearby =>
            EmConfig.Scene.NearbyRadius.Value < 0f
                ? focus.pos.ListVisibleCharas().Where(c => !c.IsPC && !c.IsAnimal).ToList()
                : focus.pos.ListCharasInRadius(focus, EmConfig.Scene.NearbyRadius.Value, c => !c.IsPC && !c.IsAnimal);
    }
}