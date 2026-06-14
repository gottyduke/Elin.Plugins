using ElinTogether.Common;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using Serilog.Context;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    /// <summary>
    ///     Send the player a map snapshot from given zone
    /// </summary>
    public void PropagateZoneChangeState(Zone zone, ISteamNetPeer? peer = null)
    {
        using var _ = LogContext.PushProperty("Zone", new { zone.ZoneFullName, ZoneUid = zone.uid }, true);

        EmpPop.Debug("Initiating zone state change");

        // NOTE (Phase 1): Removed PauseWorldStateUpdate / Resume dance.
        // Zone replication is now out-of-band; deltas & snapshots continue uninterrupted.
        var packet = ZoneDataResponse.Create(zone);

        if (peer is not null) {
            EmpLog.Debug("Dispatching zone to player {@Peer}",
                peer);

            peer.Send(packet);
        } else {
            EmpLog.Debug("Dispatching zone to all player");

            Broadcast(packet);
        }

        // update lobby data
        Session.Lobby.Current?.SetLobbyData(EmpLobbyData.CurrentZone, zone.ZoneFullName);
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
            Socket.Disconnect(peer, EmpDisconnectInfo.InvalidZone);
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

        // Staleness check (Phase 1): use ZoneFullName + ZoneUid instead of re-sending.
        // If the client is on a different zone, just ignore; it will receive the correct zone data later.
        if (response.ZoneUid != _zone.uid || response.ZoneFullName != _zone.ZoneFullName) {
            EmpLog.Debug(
                "Ignoring stale ZoneDataReceivedResponse from {@Peer} (expected {ExpectedName}/{ExpectedUid}, got {GotName}/{GotUid})",
                peer,
                _zone.ZoneFullName, _zone.uid,
                response.ZoneFullName, response.ZoneUid);
            return;
        }

        // we only move their characters to zone when they are ready
        var pos = pc.pos.GetNearestPoint(allowChara: false, allowInstalled: false);
        _zone.AddCard(chara, pos);

        EmpLog.Debug("Assigned zone sync position to player {@Peer} at {@Position}",
            peer, pos);

        // after that, their characters will always be party members
        peer.Send(new ZoneActivateResponse {
            ZoneUid = _zone.uid,
            Pos = pos,
        });
    }
}