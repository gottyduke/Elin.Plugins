using System;
using System.Linq;
using Cwl.LangMod;

namespace Cwl.Helper.Extensions;

public static class ZoneExt
{
    extension(string zoneFullName)
    {
        public bool ValidateZone(out Zone? zone, bool randomFallback = false)
        {
            var zones = EClass.game.spatials.map.Values
                .OfType<Zone>()
                .ToArray();

            var (zoneType, zoneLv) = ParseZoneFullName(zoneFullName);
            var zoneId = zoneType.Replace("Zone_", "");

            zone = zones.FirstOrDefault(z => z.GetType().Name == zoneType || z.id == zoneId)?.FindOrCreateLevel(zoneLv);

            if (zone is not null) {
                return true;
            }

            if (zoneId != "*" && !randomFallback) {
                return false;
            }

            var spawnableZones = Array.FindAll(zones, z => z.CanSpawnAdv);
            zone = spawnableZones.RandomItem();

            return zone is not null;
        }

        private (string, int) ParseZoneFullName()
        {
            var byLv = zoneFullName.LastIndexOfAny(['/', '@']);
            if (byLv == -1 || byLv >= zoneFullName.Length - 1) {
                return (zoneFullName.Replace("/", "").Replace("@", ""), 0);
            }

            var zoneLv = zoneFullName[(byLv + 1)..];
            return (
                zoneFullName[..byLv],
                zoneLv.AsInt(0)
            );
        }
    }

    extension(Zone zone)
    {
        public Zone? FindOrCreateLevel(int lv, string id = "")
        {
            try {
                var newZone = zone.FindZone(lv);
                if (newZone is not null) {
                    return newZone;
                }

                newZone = (SpatialGen.Create(id.IsEmpty(zone.GetNewZoneID(lv)), zone, true) as Zone)!;
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

    extension(Region region)
    {
        public void SpawnZoneAt(string zoneFullName, int eloX, int eloY)
        {
            if (EClass.game.spatials.Find((Zone z) => z.x == eloX && z.y == eloY) is { } existZone) {
                CwlMod.Warn<SpatialGen>("cwl_warn_exist_zone".Loc(zoneFullName, eloX, eloY, existZone.Name));
                return;
            }

            var (zoneType, zoneLv) = ParseZoneFullName(zoneFullName);
            var zoneId = zoneType.Replace("Zone_", "");
            var zoneParent = SpatialGen.Create(zoneId, region, true, eloX, eloY) as Zone;
            var zone = zoneParent?.FindOrCreateLevel(zoneLv);

            if (zone is null) {
                CwlMod.Warn<SpatialGen>($"failed to create zone: {zoneFullName}");
                return;
            }

            zone.x = eloX;
            zone.y = eloY;

            zone.parent?.RemoveChild(zone);
            region.elomap.SetZone(eloX, eloY, zone, true);
            region.AddChild(zone);

            CwlMod.Log<SpatialGen>($"created zone: {zoneFullName} at {eloX}, {eloY}");
        }
    }
}