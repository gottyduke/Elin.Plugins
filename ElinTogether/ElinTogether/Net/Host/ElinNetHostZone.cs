using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
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
        this.StartDeferredCoroutine(ResumeWorldStateUpdate);

        var peers = peer is not null
            ? [peer]
            : Socket.Peers;

        var packet = new ZoneDataResponse {
            Map = MapDataResponse.Create(zone, true),
            Zone = LZ4Bytes.Create(zone),
        };

        foreach (var remotePeer in peers) {
            var remoteChara = GetRemoteCharaFromPeer(remotePeer);
            if (remoteChara is null) {
                EmpLog.Warning("Player {@Peer} has not been assigned a remote chara",
                    remotePeer);

                Socket.Disconnect(remotePeer, "emp_invalid_remote_chara");
                return;
            }

            if (!remoteChara.ExistsOnMap) {
                var pos = pc.pos.GetNearestPoint(allowChara: false, allowInstalled: false);
                _zone.AddCard(remoteChara, pos);
                ActiveRemoteCharas.Add(remoteChara);

                EmpLog.Debug("Assigned zone sync position to player {@Peer}",
                    remotePeer);
            }

            EmpLog.Debug("Dispatching zone to peer {@Peer}",
                remotePeer);

            var moveZoneDelta = new CharaMoveZoneDelta {
                Owner = remoteChara,
                ZoneFullName = zone.ZoneFullName,
                ZoneUid = zone.uid,
                PosX = remoteChara.pos.x,
                PosZ = remoteChara.pos.z,
            };

            Delta.AddRemote(moveZoneDelta);

            remotePeer.Send(packet);
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