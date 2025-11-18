using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
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
            EmpPop.Debug("Server can only be started after claiming a zone");
            return;
        }

        Session.Lobby.CreateLobby(SteamNetLobbyType.Public);
        Session.Lobby.Current?.SetLobbyData("CurrentZone", _zone.NameWithLevel);

        if (localUdp) {
            Socket.StartServerUdp();
        } else {
            Socket.StartServerSdr();
        }

        Scheduler.Subscribe(DisconnectInactive, 1);

        // host also registers self state
        var selfState = States[0] = new() {
            Index = 0,
            Uid = (ulong)SteamUser.GetSteamID(),
            Name = SteamFriends.GetPersonaName(),
            CharaUid = player.uidChara,
        };

        // setup session states
        Session.Player = pc;
        Session.CurrentPlayers.Add(selfState);
        Session.SharedSpeed = SharedSpeed;

        EmpPop.Debug("Started server\nSource validations enabled: {SourceValidations}",
            SourceValidationsEnabled.Count);

        StartWorldStateUpdate();
    }

    protected override void RegisterPackets()
    {
        Router.RegisterHandler<SourceListResponse>(OnSourceListResponse);
        Router.RegisterHandler<MapDataRequest>(OnMapDataRequest);
        Router.RegisterHandler<ZoneDataReceivedResponse>(OnZoneDataReceivedResponse);
        Router.RegisterHandler<WorldStateRequest>(OnWorldStateRequest);
        Router.RegisterHandler<WorldStateDeltaList>(OnWorldStateDeltaResponse);
        Router.RegisterHandler<CharaStateSnapshot>(OnClientRemoteCharaSnapshot);
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
                Socket.Disconnect(peer, "emp_inactive");
            }
        }

        // remove all left over chara
        foreach (var chara in _map.charas.ToArray()) {
            if (chara.GetFlagValue("remote_chara") > 0 && !ActiveRemoteCharas.Values.Contains(chara)) {
                RemoveRemoteChara(chara);
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

        DebugProgress ??= ProgressIndicator.CreateProgress(() => new(BuildDebugInfo()), _ => false, 1f);
    }

    protected override void OnPeerDisconnected(ISteamNetPeer peer, string disconnectInfo)
    {
        EmpPop.Information("Player {@Peer} disconnected\n{DisconnectInfo}",
            peer, disconnectInfo);

        // remove left over chara
        if (States.Remove(peer.Id, out var state) &&
            ActiveRemoteCharas.Remove(peer.Id, out var remoteChara)) {
            RemoveRemoteChara(remoteChara);

            Session.CurrentPlayers.Remove(state);

            EmpLog.Debug("Removed remote chara {@Chara}",
                new {
                    Uid = remoteChara.uid,
                    remoteChara.Name,
                    Player = state.Name,
                });
        }

        // keep ticking but no update
        if (States.Count == 0) {
            PauseWorldStateUpdate();
            DebugProgress?.Kill();
        }
    }

#endregion
}