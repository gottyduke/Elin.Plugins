using System;
using Cwl.LangMod;

namespace Cwl.Helper.Extensions;

public static class ZoneExt
{
    public static Zone? FindOrCreateZone(this Zone zone, int lv)
    {
        try {
            var newZone = zone.FindZone(lv);
            if (newZone is not null) {
                return newZone;
            }

            newZone = (SpatialGen.Create(zone.GetNewZoneID(lv), zone, true) as Zone)!;
            newZone.lv = lv;
            newZone.x = zone.x;
            newZone.y = zone.y;

            CwlMod.Log<SpatialGen>("cwl_log_spatial_gen".Loc(zone.Name, lv));

            return newZone; 
        } catch (Exception ex) {
            CwlMod.Warn<SpatialGen>("cwl_error_failure".Loc(ex.Message));
            return null;
            // noexcept
        }
    }
}