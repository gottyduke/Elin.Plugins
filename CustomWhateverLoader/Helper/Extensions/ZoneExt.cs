using System;
using System.Linq;
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

    public static bool ValidateZone(this string zoneFullName, out Zone? zone, bool randomFallback = false)
    {
        var zones = EClass.game.spatials.map.Values
            .OfType<Zone>()
            .ToArray();

        var (matchZone, byLv) = ParseZoneFullName(zoneFullName);
        var byId = matchZone.Replace("Zone_", "");

        zone = zones.FirstOrDefault(z => z.GetType().Name == matchZone || z.id == byId)?.FindOrCreateZone(byLv);

        if (zone is not null) {
            return true;
        }

        if (byId != "*" && !randomFallback) {
            return false;
        }

        var spawnableZones = Array.FindAll(zones, z => z.CanSpawnAdv);
        zone = spawnableZones.RandomItem();

        return zone is not null;
    }

    private static (string, int) ParseZoneFullName(string zoneFullName)
    {
        var byLv = zoneFullName.LastIndexOfAny(['/', '@']);
        if (byLv == -1 || byLv >= zoneFullName.Length - 1) {
            return (zoneFullName.Replace("/", "").Replace("@", ""), 0);
        }

        var lv = zoneFullName[(byLv + 1)..];
        return (
            zoneFullName[..byLv],
            lv.AsInt(0)
        );
    }
}