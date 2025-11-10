using Cwl.Helper.Extensions;
using ElinTogether.Models;
using ReflexCLI.Attributes;
using Serilog.Context;

namespace ElinTogether.Net;

[ConsoleCommandClassCustomizer("emp")]
internal partial class ElinNetClient
{
    public void RequestZoneState(string zoneFullName = "", int uid = -1)
    {
        EmpLog.Information("Requesting zone state {@Zone} from host",
            new {
                ZoneFullName = zoneFullName,
                ZoneUid = uid,
            });

        Socket.FirstPeer.Send(MapDataRequest.CurrentRemoteZone);
    }

    /// <summary>
    ///     Zone change state received, update regular map assets
    /// </summary>
    private void OnZoneDataResponse(ZoneDataResponse response)
    {
        using var _ = LogContext.PushProperty("Zone", response.Map, true);

        var zoneFullName = response.Map.ZoneFullName;
        var zoneUid = response.Map.ZoneUid;

        response.Map.WriteToTemp();

        var spatial = game.spatials;

        var remoteZone = spatial.Find(zoneUid);
        if (remoteZone is null) {
            EmpLog.Information("Remote zone does not exist, creating new spatial");

            var probeZone = response.Zone.Decompress<Zone>();
            var parent = spatial.Find(probeZone.parent.uid);

            if (parent is null) {
                EmpLog.Warning("Remote zone parent does not exist in current game");

                Socket.Disconnect(Socket.FirstPeer, "emp_invalid_zone_retry");
                // TODO: add reconnect logic to resync save probe
                // Reconnect();
                return;
            }

            var (_, zoneId, zoneLv) = zoneFullName.ParseZoneFullName();
            remoteZone = SpatialGen.Create(zoneId, parent, true, probeZone.x, probeZone.y) as Zone;
            remoteZone = remoteZone.FindOrCreateLevel(zoneLv, probeZone.id);

            if (parent is Region region) {
                region.elomap.SetZone(probeZone.x, probeZone.y, remoteZone, true);
            }
        }

        if (remoteZone is null || remoteZone.ZoneFullName != zoneFullName) {
            EmpLog.Warning("Zone state mismatch");

            Socket.Disconnect(Socket.FirstPeer, "emp_invalid_zone");
            return;
        }

        EmpLog.Information("Received zone state");

        NetSession.Instance.CurrentZone = remoteZone;
        remoteZone.isGenerated = true;
    }
}