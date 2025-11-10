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
        Router.RegisterHandler<SaveDataProbe>(OnSaveDataProbe);
        Router.RegisterHandler<ZoneDataResponse>(OnZoneDataResponse);
        Router.RegisterHandler<WorldStateSnapshot>(OnWorldStateSnapshot);
        Router.RegisterHandler<WorldStateDeltaList>(OnWorldStateDeltaResponse);
    }

#region Net Events

    protected override void OnPeerConnected(ISteamNetPeer host)
    {
        EmpPop.Information("Connected to host {@Peer}",
            host);

        StartWorldStateUpdate();

        DebugProgress ??= ProgressIndicator.CreateProgress(() => new(BuildDebugInfo()), _ => false, 1f);
    }

    protected override void OnPeerDisconnected(ISteamNetPeer host, string disconnectInfo)
    {
        StopWorldStateUpdate();

        if (core.IsGameStarted) {
            game.Kill();
            scene.Init(Scene.Mode.Title);
        }

        if (ReflexUIManager.IsConsoleOpen()) {
            ReflexUIManager.StaticClose();
        }

        EmpPop.Information("Disconnected from host\n{DisconnectInfo}",
            disconnectInfo);

        NetSession.Instance.RemoveComponent();
    }

#endregion
}