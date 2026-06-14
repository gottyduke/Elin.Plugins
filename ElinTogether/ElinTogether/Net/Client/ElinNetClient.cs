using System.Diagnostics;
using ElinTogether.Common;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using ReflexCLI.UI;
using Steamworks;

namespace ElinTogether.Net;

internal partial class ElinNetClient : ElinNetBase
{
    public override bool IsHost => false;
    public ISteamNetPeer Host => Socket.FirstPeer;

    public void ConnectLocalPort(ushort port = EmpConstants.LocalPort)
    {
        Stop();
        Socket.Connect(port);
    }

    public void ConnectSteamUser(ulong steamId)
    {
        Stop();
        // store for potential rejoin after transient disconnect
        Session.LastSession = new() { HostSteamId = steamId };
        Socket.Connect(new CSteamID(steamId));
    }

    protected override void RegisterPackets()
    {
        // delta
        Router.RegisterHandler<ZoneDataResponse>(OnZoneDataResponse);
        Router.RegisterHandler<ZoneActivateResponse>(OnZoneActivateResponse);
        Router.RegisterHandler<WorldStateSnapshot>(OnWorldStateSnapshot);
        Router.RegisterHandler<WorldStateDeltaList>(OnWorldStateDeltaResponse);

        // source validation
        Router.RegisterHandler<SourceValidationRequest>(OnSourceValidationRequest);
        Router.RegisterHandler<SourceListSync>(OnSourceListSync);

        // session
        Router.RegisterHandler<SessionNewPlayerRequest>(OnSessionNewPlayerRequest);
        Router.RegisterHandler<SessionRejoinResponse>(OnSessionRejoinResponse);
        Router.RegisterHandler<SaveDataProbe>(OnSaveDataProbe);
        Router.RegisterHandler<SteamLobbyRequest>(OnSteamLobbyRequest);
        Router.RegisterHandler<SessionPlayersSnapshot>(OnSessionStatesUpdate);
        Router.RegisterHandler<NetSessionRules>(OnSessionRulesUpdate);
    }

    protected override void DisconnectInactive()
    {
    }

    internal override void Stop()
    {
        base.Stop();

        if (!core.IsGameStarted) {
            return;
        }

        scene.Init(Scene.Mode.Title);
    }

#region Net Events

    /// <summary>
    ///     Net event: On connected to host
    /// </summary>
    protected override void OnPeerConnected(ISteamNetPeer host)
    {
        Session.SetPhase(ConnectionPhase.Authenticating);
        EmpPop.Information("Connecting to host {@Peer}",
            Host);

        // CLIENT-ONLY
        var sw = Stopwatch.StartNew();
        while (host.Name is null && sw.ElapsedMilliseconds <= 500) {
            // do a spin wait here to pin the username
            // ignore if steam can't respond in 500ms
        }

        this.StartDeferredCoroutine(StartWorldStateUpdate, () => core.IsGameStarted);

#if DEBUG
        DebugProgress ??= EGui.CreatePopup(() => new(BuildDebugInfo()), _ => false, 1f);
#endif

        // Rejoin path: if we are attempting to resume an existing session, declare intent early.
        // Host will short-circuit PreparePlayerJoin if it has a saved remote chara for us.
        if (Session.CurrentPhase == ConnectionPhase.Reconnecting &&
            Session.LastSession is { CharaUid: > 0 } last) {
            Host.Send(new SessionRejoinRequest {
                LastKnownServerTick = last.LastServerTick,
                CharaUid = last.CharaUid,
                LastZoneUid = last.LastZoneUid,
            });
            EmpLog.Debug("Sent SessionRejoinRequest for chara {CharaUid}", last.CharaUid);
        }
    }

    /// <summary>
    ///     Net event: On disconnected from host.
    ///     On transient DC while Synchronized, attempt lightweight rejoin instead of
    ///     forcing title + full reconnect.
    /// </summary>
    protected override void OnPeerDisconnected(ISteamNetPeer host, string disconnectInfo)
    {
        StopWorldStateUpdate();
        StopAllCoroutines();

        if (ReflexUIManager.IsConsoleOpen()) {
            ReflexUIManager.StaticClose();
        }

        // Only attempt reconnect if we were fully synchronized (happy path).
        // Otherwise fall back to title.
        if (Session.CurrentPhase == ConnectionPhase.Synchronized &&
            Session.LastSession is { HostSteamId: > 0 } last) {
            Session.SetPhase(ConnectionPhase.Reconnecting);
            EmpPop.Information("Connection lost. Attempting to rejoin...");

            // keep local game state; do not call RemoveComponent / go to title yet
            ConnectSteamUser(last.HostSteamId);
            return;
        }

        // Non-recoverable path: clean up and return to title
        if (core.IsGameStarted) {
            scene.Init(Scene.Mode.Title);
        }

        EmpPop.Information("Disconnected from host\n{DisconnectInfo}",
            disconnectInfo);

        Session.RemoveComponent();
    }

#endregion
}