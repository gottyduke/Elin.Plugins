using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Authentication;
using Cwl.Helper.Unity;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost : ElinNetBase
{
    internal readonly ConcurrentDictionary<uint, NetPeerState> States = [];

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

    private void EnsureValidation(ISteamNetPeer peer)
    {
        if (!States.TryGetValue(peer.Id, out var state) ||
            !state.IsValidated) {
            throw new InvalidCredentialException("peer is not validated");
        }
    }

    private void Broadcast<T>(T packet)
    {
        Socket.Broadcast.Send(packet);
    }

#region Net Events

    protected override void OnPeerConnected(ISteamNetPeer peer)
    {
        EmpPop.Information("Player {@Peer} connected", peer);

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