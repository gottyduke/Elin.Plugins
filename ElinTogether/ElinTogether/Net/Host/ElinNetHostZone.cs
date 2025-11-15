using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using Serilog.Context;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    /// <summary>
    /// Send the player a map snapshot from given zone
    /// </summary>
    public void PropagateZoneChangeState(Zone zone, ISteamNetPeer? peer = null)
    {
        using var _ = LogContext.PushProperty("Zone", new { zone.ZoneFullName, ZoneUid = zone.uid }, true);

        EmpPop.Debug("Initiating zone state change");

        PauseWorldStateUpdate();
        // try not to drop deltas during loading or something
        this.StartDeferredCoroutine(() => ResumeWorldStateUpdate(true));

        var packet = ZoneDataResponse.Create(zone);

        if (peer is not null) {
            EmpLog.Debug("Dispatching zone to player {@Peer}",
                peer);

            peer.Send(packet);
        } else {
            EmpLog.Debug("Dispatching zone to all player");

            Broadcast(packet);
        }
    }

    /// <summary>
    ///     Net event: Send the clients a map snapshot
    /// </summary>
    private void OnMapDataRequest(MapDataRequest request, ISteamNetPeer peer)
    {
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

            // gtfo
            Socket.Disconnect(peer, "emp_invalid_zone");
        }
    }

    /// <summary>
    ///     Net event: Clients have replicated the zone and map, ready to transition
    /// </summary>
    private void OnZoneDataReceivedResponse(ZoneDataReceivedResponse response, ISteamNetPeer peer)
    {
        EmpLog.Debug("Player {@Peer} has finished zone replication",
            peer);

        var chara = ActiveRemoteCharas[peer.Id];

        // check if the received zone is still the current zone
        // in case clients have a high RTT and host fast fingered to another zone
        if (response.ZoneUid != _zone.uid) {
            EmpLog.Debug("...but the zone state is stale, switching to new zone state {@Zone}",
                new {
                    _zone.ZoneFullName,
                    ZoneUid = _zone.uid,
                });

            PropagateZoneChangeState(_zone, peer);
            return;
        }

        // we only move their character to zone when they are ready
        var pos = pc.pos.GetNearestPoint(allowChara: false, allowInstalled: false);
        _zone.AddCard(chara, pos);

        EmpLog.Debug("Assigned zone sync position to player {@Peer} at {@Position}",
            peer, pos);

        // after that, their characters will always be with host as party members
        peer.Send(new ZoneActivateResponse {
            ZoneUid = _zone.uid,
            Pos = pos,
        });
    }
}