using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Cwl.Helper.Unity;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost : ElinNetBase
{
    internal readonly ConcurrentDictionary<int, NetPeerState> States = [];

    public override bool IsHost => true;

    internal void StartServer()
    {
        Stop();
        StopWorldStateUpdate();

        if (!core.IsGameStarted || player?.chara?.homeBranch?.owner is null) {
            EmpPop.Debug("Server can only be started after claiming a zone");
            return;
        }

        Socket.StartServerSdr();
        Scheduler.Subscribe(DisconnectInactive, 1);

        // TODO Assign SessionId
        NetSession.Instance.SessionId = 0UL;
        NetSession.Instance.SharedSpeed = SharedSpeed;

        EmpPop.Debug("Started server via SDR\nSource validations enabled: {SourceValidations}",
            SourceValidationsEnabled.Count);

        StartWorldStateUpdate();
    }

    protected override void RegisterPackets()
    {
        Router.RegisterHandler<SourceListResponse>(OnSourceListResponse);
        Router.RegisterHandler<MapDataRequest>(OnMapDataRequest);
        Router.RegisterHandler<WorldStateRequest>(OnWorldStateRequest);
        Router.RegisterHandler<WorldStateDeltaList>(OnWorldStateDeltaResponse);
        Router.RegisterHandler<RemoteCharaSnapshot>(OnClientRemoteCharaSnapshot);
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
            if (!peer.IsConnected && NetSession.Instance.Tick - state.LastReceivedTick > 25) {
                Socket.Disconnect(peer, "emp_inactive");
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

        if (SourceValidationsEnabled.Count > 0) {
            RequestSourceValidation(peer);
        } else {
            PreparePlayerJoin(peer);
        }

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