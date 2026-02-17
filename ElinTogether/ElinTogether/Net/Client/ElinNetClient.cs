using System.Diagnostics;
using Cwl.Helper.Unity;
using ElinTogether.Common;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using ReflexCLI.UI;
using Steamworks;

namespace ElinTogether.Net;

internal partial class ElinNetClient : ElinNetBase
{
    public override bool IsHost => false;

    public void ConnectLocalPort(ushort port = EmpConstants.LocalPort)
    {
        Stop();
        Socket.Connect(port);
    }

    public void ConnectSteamUser(ulong steamId)
    {
        Stop();
        Socket.Connect(new CSteamID(steamId));
    }

    protected override void RegisterPackets()
    {
        Router.RegisterHandler<SourceListRequest>(OnSourceListRequest);
        Router.RegisterHandler<SourceDiffResponse>(OnSourceDiffResponse);
        Router.RegisterHandler<SessionNewPlayerRequest>(OnSessionNewPlayerRequest);
        Router.RegisterHandler<SaveDataProbe>(OnSaveDataProbe);
        Router.RegisterHandler<SteamLobbyRequest>(OnSteamLobbyRequest);
        Router.RegisterHandler<SessionPlayersSnapshot>(OnSessionStatesUpdate);
        Router.RegisterHandler<ZoneDataResponse>(OnZoneDataResponse);
        Router.RegisterHandler<ZoneActivateResponse>(OnZoneActivateResponse);
        Router.RegisterHandler<WorldStateSnapshot>(OnWorldStateSnapshot);
        Router.RegisterHandler<WorldStateDeltaList>(OnWorldStateDeltaResponse);
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

        game.Kill();
        scene.Init(Scene.Mode.Title);
    }

#region Net Events

    /// <summary>
    ///     Net event: On connected to host
    /// </summary>
    protected override void OnPeerConnected(ISteamNetPeer host)
    {
        // CLIENT-ONLY
        var sw = Stopwatch.StartNew();
        while (host.Name is null && sw.ElapsedMilliseconds <= 500) {
            // do a spin wait here to pin the username
            // ignore if steam can't respond in 500ms
        }

        EmpPop.Information("Connecting to host {@Peer}",
            Socket.FirstPeer);

        this.StartDeferredCoroutine(StartWorldStateUpdate, () => core.IsGameStarted);

        DebugProgress ??= ProgressIndicator.CreateProgress(() => new(BuildDebugInfo()), _ => false, 1f);
    }

    /// <summary>
    ///     Net event: On disconnected from host
    /// </summary>
    protected override void OnPeerDisconnected(ISteamNetPeer host, string disconnectInfo)
    {
        StopWorldStateUpdate();
        StopAllCoroutines();

        if (core.IsGameStarted) {
            game.Kill();
            scene.Init(Scene.Mode.Title);
        }

        if (ReflexUIManager.IsConsoleOpen()) {
            ReflexUIManager.StaticClose();
        }

        EmpPop.Information("Disconnected from host\n{DisconnectInfo}",
            disconnectInfo);

        Session.RemoveComponent();
    }

#endregion
}