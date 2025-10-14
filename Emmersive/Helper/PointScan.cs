using System.Collections.Generic;

namespace Emmersive.Helper;

public static class PointScan
{
    extension(Chara focus)
    {
        public List<Chara> Nearby =>
            focus.pos.ListCharasInRadius(focus, EmConfig.Scene.NearbyRadius.Value, c => !c.IsPC && !c.IsAnimal);
    }
}