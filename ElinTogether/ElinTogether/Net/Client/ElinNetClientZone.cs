using Cwl.Helper.Extensions;
using ElinTogether.Models;
using ElinTogether.Patches;
using ReflexCLI.Attributes;
using Serilog.Context;

namespace ElinTogether.Net;

[ConsoleCommandClassCustomizer("emp")]
internal partial class ElinNetClient
{
    public void RequestZoneState(MapDataRequest request)
    {
        EmpLog.Information("Requesting zone state {@Zone} from host",
            request);

        Socket.FirstPeer.Send(request);
    }

    /// <summary>
    /// Net event: received zone state update, must do a scene init
    /// </summary>
    private void OnZoneDataResponse(ZoneDataResponse response)
    {
        using var _ = LogContext.PushProperty("Zone", response.Map, true);

        response.WriteToTemp();

        var spatial = game.spatials;

        // popping from spatial gen refs makes no sense actually
        var remoteZone = spatial.Find(response.ZoneUid) ?? SpatialGenEvent.TryPop(response.ZoneUid);
        if (remoteZone is null) {
            EmpLog.Information("Remote zone does not exist, waiting for new spatial gen");

            var probeZone = response.Zone.Decompress<Zone>();
            var parent = spatial.Find(probeZone.parent.uid);

            if (parent is null) {
                EmpLog.Warning("Remote zone parent does not exist in current game");

                // TODO: add reconnect logic to resync save probe
                Socket.Disconnect(Socket.FirstPeer, "emp_invalid_zone");
                return;
            }

            // swap out the serialized parent
            probeZone.parent = parent;
            spatial.map[response.ZoneUid] = remoteZone = probeZone;

            if (parent is Region region) {
                region.elomap.SetZone(probeZone.x, probeZone.y, remoteZone, true);
            }
        }

        if (remoteZone.ZoneFullName != response.ZoneFullName) {
            EmpLog.Warning("Zone state mismatch");

            // TODO: add reconnect logic to resync save probe
            Socket.Disconnect(Socket.FirstPeer, "emp_invalid_zone");
            return;
        }

        EmpLog.Information("Received zone state");

        // suppress client-side map regeneration
        remoteZone.isGenerated = true;
        remoteZone.dateExpire = int.MaxValue;

        if (remoteZone is Region eloMap) {
            // suppress client-side overworld poi generation
            eloMap.dateCheckSites = int.MaxValue;
        }

        // update session remote zone
        NetSession.Instance.CurrentZone = remoteZone;
    }
}