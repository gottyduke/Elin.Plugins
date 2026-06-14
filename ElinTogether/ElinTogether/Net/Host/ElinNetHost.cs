using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ElinTogether.Common;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using Steamworks;

namespace ElinTogether.Net;

internal partial class ElinNetHost : ElinNetBase
{
    internal readonly Dictionary<int, NetPeerState> States = [];

    public override bool IsHost => true;

    internal void StartServer(bool localUdp = false)
    {
        Stop();
        StopWorldStateUpdate();

        if (!core.IsGameStarted || player?.chara?.homeBranch?.owner is null) {
            EmpLog.Warning("Cannot start server: game not started or no land claimed");
            EmpPop.Debug("emp_ui_unclaimed_zone".lang());
            Session.RemoveComponent();
            return;
        }

        Session.SetPhase(ConnectionPhase.LobbyCreating);
        Session.Lobby.CreateLobby(SteamNetLobbyType.Public);

        Session.SetPhase(ConnectionPhase.HostingListening);

        if (localUdp) {
            Socket.StartServerUdp();
        } else {
            Socket.StartServerSdr();
        }

        Scheduler.Subscribe(DisconnectInactive, 1);

        // host also registers self state
        var selfState = States[0] = new() {
            Index = 0,
            PeerUid = (ulong)SteamUser.GetSteamID(),
            Name = SteamFriends.GetPersonaName(),
            CharaUid = player.uidChara,
        };

        // setup session states
        Session.Player = pc;
        Session.CurrentPlayers.Add(selfState);
        Session.SharedSpeed = NetSession.Instance.Rules.UseSharedSpeed
            ? SharedSpeed
            : -1;

        Session.SetPhase(ConnectionPhase.HostingReady);
        EmpPop.Information("Started server");

        CardCache.CacheCurrentZone();

        StartWorldStateUpdate();
    }

    protected override void RegisterPackets()
    {
        Router.RegisterHandler<SessionNewPlayerResponse>(OnSessionNewPlayerResponse);
        Router.RegisterHandler<MapDataRequest>(OnMapDataRequest);
        Router.RegisterHandler<ZoneDataReceivedResponse>(OnZoneDataReceivedResponse);
        Router.RegisterHandler<WorldStateRequest>(OnWorldStateRequest);
        Router.RegisterHandler<WorldStateDeltaList>(OnWorldStateDeltaResponse);
        Router.RegisterHandler<CharaStateSnapshot>(OnClientRemoteCharaSnapshot);

        // source validation
        Router.RegisterHandler<SourceValidationResponse>(OnSourceListResponse);
        Router.RegisterHandler<SessionRejoinRequest>(OnSessionRejoinRequest);
    }

    private void Broadcast<T>(T packet)
    {
        Socket.Broadcast.Send(packet);
    }

    protected override void DisconnectInactive()
    {
        foreach (var peer in Socket.Peers) {
            if (!States.TryGetValue(peer.Id, out var state)) {
                continue;
            }

            if (state.LastReceivedTick == -1) {
                continue;
            }

            // client has not been responding after 25 ticks
            if (!peer.IsConnected && Session.Tick - state.LastReceivedTick > 25) {
                Socket.Disconnect(peer, EmpDisconnectInfo.InactivePeer);
            }
        }

        // remove all left over chara
        foreach (var chara in _map.charas.ToArray()) {
            if (chara.GetBool("remote_chara") && !ActiveRemoteCharas.Values.Contains(chara)) {
                RemoveRemoteChara(chara);
            }
        }

        // Evict stale pending reconnects (simple time-based window).
        // If a player does not rejoin within the window we finally remove the remote chara.
        var now = DateTime.UtcNow;
        var toEvict = new List<ulong>();
        foreach (var (steamUid, info) in PendingReconnects) {
            if ((now - info.DcTime).TotalSeconds > 90) { // 90s reconnect window
                toEvict.Add(steamUid);
            }
        }

        foreach (var steamUid in toEvict) {
            if (PendingReconnects.Remove(steamUid, out var stale)) {
                // find and remove the corresponding remote chara
                var deadEntry = ActiveRemoteCharas.FirstOrDefault(kv => kv.Value.uid == stale.CharaUid);
                if (deadEntry.Value is not null) {
                    ActiveRemoteCharas.Remove(deadEntry.Key);
                    RemoveRemoteChara(deadEntry.Value);
                    EmpLog.Information("Evicted pending reconnect for steam {Uid} (chara {CharaUid}) after timeout",
                        steamUid, stale.CharaUid);
                }
            }
        }
    }

#region Net Events

    protected override void OnPeerConnected(ISteamNetPeer peer)
    {
        var sw = Stopwatch.StartNew();
        while (peer.Name is null && sw.ElapsedMilliseconds <= 500) {
            // do a spin wait to pin the username
        }

        EmpPop.Information("Player {@Peer} connected",
            peer);

        // do source validations
        RequestSourceValidation(peer);

        // and invite to steam lobby if clients aren't already in
        peer.Send(new SteamLobbyRequest {
            LobbyId = (ulong)Session.Lobby.Current!.LobbyId,
        });

#if DEBUG
        DebugProgress ??= EGui.CreatePopup(() => new(BuildDebugInfo()), _ => false, 1f);
#endif
    }

    protected override void OnPeerDisconnected(ISteamNetPeer peer, string disconnectInfo)
    {
        EmpPop.Information("Player {@Peer} disconnected\n{DisconnectInfo}",
            peer, disconnectInfo);

        // Rejoin support: do NOT immediately remove the remote chara.
        // Instead, record a pending reconnect entry so the same Steam UID can
        // perform a lightweight SessionRejoinRequest / HandleRejoin later.
        if (States.Remove(peer.Id, out var state)) {
            if (ActiveRemoteCharas.TryGetValue(peer.Id, out var remoteChara)) {
                // keep the chara in the world but mark it as pending reconnect
                remoteChara.SetBool("pending_reconnect", true);

                PendingReconnects[peer.Uid] = new(
                    remoteChara.uid,
                    state.LastReceivedTick,
                    DateTime.UtcNow,
                    disconnectInfo);

                EmpLog.Information("Player {Name} marked for pending reconnect (chara {Uid}). " +
                                   "Will be evicted on timeout or successful rejoin.",
                    state.Name, remoteChara.uid);
            }

            Session.CurrentPlayers.Remove(state);
        }

        EmpLog.Debug("Player {Name} disconnected. {Remaining} players remaining, {Pending} pending reconnects",
            state?.Name ?? "unknown", States.Count, PendingReconnects.Count);

        // keep ticking but no update
        if (States.Count == 0) {
            PauseWorldStateUpdate();
            DebugProgress?.Kill();
        }
    }

#endregion
}