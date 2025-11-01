using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cwl.LangMod;

namespace Cwl.Helper.Extensions;

public static class ZoneExt
{
    extension(string zoneFullName)
    {
        public bool ValidateZone([NotNullWhen(true)] out Zone? zone, bool randomFallback = false)
        {
            var zones = EClass.game.spatials.map.Values
                .OfType<Zone>()
                .ToArray();

            var (zoneType, zoneId, zoneLv) = ParseZoneFullName(zoneFullName);
            zone = zones.FirstOrDefault(z => z.GetType().Name == zoneType || z.id == zoneId)?.FindOrCreateLevel(zoneLv);

            if (zone is not null) {
                return true;
            }

            if (zoneId != "*" && !randomFallback) {
                return false;
            }

            zone = Array.FindAll(zones, z => z.CanSpawnAdv).RandomItem();
            return zone is not null;
        }

        public (string zoneType, string zoneId, int zoneLv) ParseZoneFullName()
        {
            string zoneType;
            var zoneLv = 0;

            var byLv = zoneFullName.LastIndexOfAny(['/', '@']);
            if (byLv != -1 && byLv < zoneFullName.Length - 1) {
                zoneType = zoneFullName[..byLv];
                zoneLv = zoneFullName[(byLv + 1)..].AsInt(0);
            } else {
                zoneType = zoneFullName.Replace("/", "").Replace("@", "");
            }

            return (zoneType, zoneType.Replace("Zone_", ""), zoneLv);
        }
    }

    extension(Zone zone)
    {
        public string ZoneFullName => $"Zone_{zone.id}@{zone.lv}";

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
        public void DestroyZoneAt(int eloX, int eloY)
        {
            if (region.GetZoneAt(out var existZone, eloX, eloY)) {
                existZone.Destroy();
            }
        }

        public bool GetZoneAt([NotNullWhen(true)] out Zone? existZone, int eloX, int eloY)
        {
            existZone = EClass.game.spatials.Find((Zone z) => z.x == eloX && z.y == eloY);
            return existZone is not null;
        }

        public void SpawnZoneAt(string zoneFullName, int eloX, int eloY, bool forceSpawn = true)
        {
            if (region.GetZoneAt(out var existZone, eloX, eloY)) {
                if (forceSpawn) {
                    existZone.Destroy();
                } else {
                    CwlMod.WarnWithPopup<SpatialGen>("cwl_warn_exist_zone".Loc(zoneFullName, eloX, eloY, existZone.Name));
                    return;
                }
            }

            var (_, zoneId, zoneLv) = ParseZoneFullName(zoneFullName);
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

            CwlMod.Log<SpatialGen>($"created zone: {zoneFullName} at {eloX}, {eloY}");
        }
    }
}