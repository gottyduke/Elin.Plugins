using Cwl.Helper.Extensions;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class SpatialGenDelta : ElinDeltaBase
{
    [Key(0)]
    public required string ZoneFullName { get; init; }

    [Key(1)]
    public required int ZoneUid { get; init; }

    [Key(2)]
    public required Position Pos { get; init; }

    [Key(3)]
    public required int ParentZoneUid { get; init; }

    [Key(4)]
    public int Icon { get; init; }

    public static SpatialGenDelta Create(Zone zone)
    {
        return new() {
            ZoneFullName = zone.ZoneFullName,
            ZoneUid = zone.uid,
            Pos = new() { X = zone.x, Z = zone.y },
            ParentZoneUid = zone.parent?.uid ?? -1,
            Icon = zone.icon,
        };
    }

    public override void Apply(ElinNetBase net)
    {
        if (net.IsHost) {
            // reject every single zone creation from clients
            return;
        }

        var remoteZone = game.spatials.Find(ZoneUid);
        if (remoteZone?.ZoneFullName == ZoneFullName && remoteZone.uid == ZoneUid) {
            // we already handled this on zone data response code
            return;
        }

        var (_, zoneId, zoneLv) = ZoneFullName.ParseZoneFullName();
        var parent = game.spatials.Find(ParentZoneUid);
        remoteZone = SpatialGen.Create(zoneId, parent, true, Pos.X, Pos.Z) as Zone;

        if (remoteZone is null) {
            return;
        }

        // update on overworld
        if (parent is Region region) {
            region.elomap.SetZone(Pos.X, Pos.Z, remoteZone, true);
        }

        SpatialGenEvent.HeldRefZones[ZoneUid] = remoteZone;
    }
}