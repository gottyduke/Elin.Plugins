using System.Collections.Generic;

namespace Emmersive.Helper;

public static class PointScan
{
    public static List<Chara> LastNearby { get; private set; } = [];

    extension(Chara focus)
    {
        public List<Chara> Nearby =>
            LastNearby = focus.pos.ListCharasInRadius(focus, EmConfig.Context.NearbyRadius.Value, c => !c.IsPC && !c.IsAnimal);
    }
}