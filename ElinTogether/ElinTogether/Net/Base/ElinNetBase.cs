using Cwl.Helper.String;
using Cwl.Helper.Unity;
using ElinTogether.Helper;
using ElinTogether.Net.Steam;
using ReflexCLI.UI;

namespace ElinTogether.Net;

public abstract partial class ElinNetBase : EMono
{
    protected static readonly NetSession Session = NetSession.Instance;
    public readonly ElinDeltaManager Delta = new();
    protected readonly SteamNetMessageRouter Router = new();
    protected readonly TickScheduler Scheduler = new();
    protected readonly SteamNetManager Socket = new();
    private bool _initialized;
    protected ProgressIndicator? DebugProgress;

    public abstract bool IsHost { get; }

    public bool IsConnected => Socket.IsConnected;

    private void Awake()
    {
        Initialize();

#if !DEBUG
        if (!HarmonyLib.Harmony.HasAnyPatches(ModInfo.Guid)) {
            EmpMod.SharedHarmony.PatchAll(EmpMod.Assembly);
        }
#endif
    }

    private void Update()
    {
        Scheduler.Tick();
        Socket.Poll();
    }

    private void OnDestroy()
    {
        Stop();
        Socket.Dispose();
        NetSession.Instance.Lobby.LeaveLobby();

#if !DEBUG
        EmpMod.SharedHarmony.UnpatchSelf();
#endif
    }

    protected abstract void OnPeerConnected(ISteamNetPeer peer);

    protected abstract void OnPeerDisconnected(ISteamNetPeer peer, string reason);

    protected abstract void RegisterPackets();

    protected abstract void DisconnectInactive();

    protected void Initialize()
    {
        if (_initialized) {
            return;
        }

        Router.OnPeerConnectedEvent += OnPeerConnected;
        Router.OnPeerDisconnectedEvent += OnPeerDisconnected;

        Socket.Initialize(Router);

        RegisterPackets();

        CreateValidation();

        _initialized = true;
    }

    internal virtual void Stop()
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
            sb.AppendLine(peer.Stat.ToString());
        }

        sb.Append(Delta.ToString());

        return sb.ToString();
    }
}