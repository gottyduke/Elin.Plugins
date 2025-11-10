using Cwl.Helper.String;
using Cwl.Helper.Unity;
using ElinTogether.Helper;
using ElinTogether.Net.Steam;
using ReflexCLI.UI;

namespace ElinTogether.Net;

public abstract partial class ElinNetBase : EMono
{
    public readonly ElinDeltaManager Delta = new();
    private bool _initialized;
    protected ProgressIndicator? DebugProgress;
    protected SteamNetMessageRouter Router = null!;
    protected TickScheduler Scheduler = null!;
    protected SteamNetManager Socket = null!;

    public abstract bool IsHost { get; }

    public bool IsConnected => Socket.IsConnected;

    private void Awake()
    {
        Initialize();
    }

    private void FixedUpdate()
    {
        Socket.Poll();
        Scheduler.Tick();
    }

    private void OnDestroy()
    {
        Stop();
        Socket.Dispose();
    }

    protected abstract void OnPeerConnected(ISteamNetPeer peer);

    protected abstract void OnPeerDisconnected(ISteamNetPeer peer, string reason);

    protected abstract void RegisterPackets();

    protected void Initialize()
    {
        if (_initialized) {
            return;
        }

        Router = new();
        Router.OnPeerConnectedEvent += OnPeerConnected;
        Router.OnPeerDisconnectedEvent += OnPeerDisconnected;

        Socket = new();
        Socket.Initialize(Router);

        Scheduler = new();
        Scheduler.Subscribe(DisconnectInactive, 1);

        RegisterPackets();

        CreateValidation();

        _initialized = true;
    }

    internal void Stop()
    {
        if (ReflexUIManager.IsConsoleOpen()) {
            ReflexUIManager.StaticClose();
        }

        Socket.Stop();
        DebugProgress?.Kill();
    }

    protected string BuildDebugInfo()
    {
        using var sb = StringBuilderPool.Get();

        var peers = Socket.Peers;
        for (var i = 0; i < peers.Count; ++i) {
            var peer = peers[i];
            sb.AppendLine(peer.Colorize(peer.Name));

            sb.Append(peers[i].Stat.ToString());

            if (i < peers.Count - 1) {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private void DisconnectInactive()
    {
        foreach (var peer in Socket.Peers) {
            if (!peer.IsConnected) {
                Socket.Disconnect(peer, "emp_inactive");
            }
        }

        // remove self
        if (!IsHost && Socket.Peers.Count == 0) {
            NetSession.Instance.RemoveComponent();
        }
    }
}