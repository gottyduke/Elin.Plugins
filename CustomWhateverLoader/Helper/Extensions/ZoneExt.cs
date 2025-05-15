namespace Cwl.Helper.Extensions;

public static class ZoneExt
{
    public static Zone FindOrCreateZone(this Zone zone, int lv)
    {
        var newZone = zone.FindZone(lv);
        if (newZone is not null) {
            return newZone;
        }

        newZone = (SpatialGen.Create(zone.GetNewZoneID(lv), zone, true) as Zone)!;
        newZone.lv = lv;
        newZone.x = zone.x;
        newZone.y = zone.y;

        CwlMod.Log<SpatialGen>($"instantiating new zone {zone.Name} / {lv}");

        return newZone;
    }
}