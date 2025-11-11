using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using Serilog.Context;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    internal void PropagateZoneChangeState(Zone zone, ISteamNetPeer? peer = null)
    {
        using var _ = LogContext.PushProperty("Zone", zone, true);

        EmpPop.Debug("Initiating zone state change");

        PauseWorldStateUpdate();
        // try not to drop deltas during loading or something
        this.StartDeferredCoroutine(() => ResumeWorldStateUpdate(false));

        var packet = new ZoneDataResponse {
            Map = MapDataResponse.Create(zone, true),
            Zone = LZ4Bytes.Create(zone),
        };

        if (peer is not null) {
            EmpLog.Debug("Dispatching zone to player {@Peer}",
                peer);

            peer.Send(packet);
        } else {
            EmpLog.Debug("Dispatching zone to all player");

            Broadcast(packet);
        }
    }

    private void OnMapDataRequest(MapDataRequest request, ISteamNetPeer peer)
    {
        EnsureValidation(peer);

        using var _ = LogContext.PushProperty("Zone", request, true);

        EmpLog.Information("Received zone state request from player {@Peer}",
            peer);

        var zone = _zone;
        if (request.ZoneUid != -1) {
            zone = game.spatials.Find(request.ZoneUid) ??
                   game.spatials.Find<Zone>(z => z.ZoneFullName == request.ZoneFullName);
        }

        if (zone is not null) {
            PropagateZoneChangeState(zone, peer);
        } else {
            EmpLog.Warning("Player {@Peer} requested invalid zone state",
                peer);

            Socket.Disconnect(peer, "emp_invalid_zone");
        }
    }
}