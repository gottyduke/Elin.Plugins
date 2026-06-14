using System;
using System.Collections.Generic;
using System.Linq;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    private readonly Dictionary<ulong, SessionRejoinRequest> _pendingRejoinIntents = [];
    public readonly Dictionary<ulong, PendingReconnectInfo> PendingReconnects = [];

    private bool IsCharaPendingReconnect(int uid)
    {
        return PendingReconnects.Values.Any(p => p.CharaUid == uid);
    }

    /// <summary>
    ///     Net event: Client rejoin
    /// </summary>
    private void OnSessionRejoinRequest(SessionRejoinRequest request, ISteamNetPeer peer)
    {
        EmpLog.Information("Rejoin request from {@Peer} for chara {CharaUid} (lastTick={Tick})",
            peer, request.CharaUid, request.LastKnownServerTick);

        _pendingRejoinIntents[peer.Uid] = request;

        // if we already have a saved chara for this user, we can attempt rejoin
        if (SavedRemoteCharas.ContainsKey(peer.Uid) ||
            PendingReconnects.ContainsKey(peer.Uid)) {
            HandleRejoin(peer, request);
        } else {
            // no saved remote for this steam user -> cannot rejoin.
            peer.Send(new SessionRejoinResponse {
                Success = false,
                Reason = "no_saved_remote_chara", // TODO: add loc string
            });
            _pendingRejoinIntents.Remove(peer.Uid);
        }
    }

    private void HandleRejoin(ISteamNetPeer peer, SessionRejoinRequest req)
    {
        if (!SavedRemoteCharas.TryGetValue(peer.Uid, out var savedUid) ||
            game.cards.globalCharas.Find(savedUid) is not { } chara) {
            EmpLog.Warning("Rejoin failed: no saved chara for peer {Peer}", peer);
            peer.Send(new SessionRejoinResponse { Success = false, Reason = "chara_not_found" });
            _pendingRejoinIntents.Remove(peer.Uid);
            PendingReconnects.Remove(peer.Uid);
            return;
        }

        if (req.CharaUid != 0 && req.CharaUid != chara.uid) {
            EmpLog.Warning("Rejoin chara uid mismatch: requested {Req} vs saved {Saved}", req.CharaUid, chara.uid);
        }

        chara.MakeAlly();
        if (chara.currentZone != pc.currentZone) {
            chara.MoveZone(pc.currentZone);
        }
        chara.SetBool("remote_chara", true);
        chara.SetBool("pending_reconnect", false);

        ActiveRemoteCharas[peer.Id] = chara;

        var state = States[peer.Id] = new() {
            Index = peer.Id,
            PeerUid = peer.Uid,
            Name = peer.Name ?? "rejoined",
            CharaUid = chara.uid,
            LastReceivedTick = req.LastKnownServerTick >= 0 ? req.LastKnownServerTick : Session.Tick,
        };

        if (Session.CurrentPlayers.All(s => s.PeerUid != peer.Uid)) {
            Session.CurrentPlayers.Add(state);
        }

        PendingReconnects.Remove(peer.Uid);
        _pendingRejoinIntents.Remove(peer.Uid);

        peer.Send(new SessionRejoinResponse {
            Success = true,
            CurrentServerTick = Session.Tick,
            CurrentZoneUid = Session.CurrentZone?.uid,
            CurrentZoneFullName = Session.CurrentZone?.ZoneFullName,
        });

        EmpLog.Information("Rejoined peer {@Peer} to existing remote chara {Name} ({Uid})",
            peer, chara.Name, chara.uid);
    }

    public record struct PendingReconnectInfo(int CharaUid, long LastTick, DateTime DcTime, string DisconnectInfo);
}