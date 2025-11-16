using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ElinTogether.Common;
using ElinTogether.Helper;
using Steamworks;

namespace ElinTogether.Net.Steam;

public partial class SteamNetManager(ISteamNetSerializer? serializer = null) : IDisposable
{
    private static readonly long _connectionKey = BuildVersionIntegrity.VersionStringToLong(ModInfo.BuildVersion);

    private static readonly SteamNetworkingConfigValue_t _connectionKeyConfig = new() {
        m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_ConnectionUserData,
        m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int64,
        m_val = new() {
            m_int64 = _connectionKey,
        },
    };

    private readonly IntPtr[] _batchedMessages = new IntPtr[EmpConstants.MaxBatchedMessages];
    private readonly SteamNetPeerBroadcast _broadcast = new(serializer ?? new SteamNetSerializer());
    private readonly List<SteamNetPeer> _peers = [];
    private readonly ISteamNetSerializer _serializer = serializer ?? new SteamNetSerializer();

    private bool _disposed;
    private ISteamNetListener? _listener;
    private HSteamListenSocket _listenSocket;
    private HSteamNetPollGroup _pollGroup;

    public bool IsHost { get; private set; }
    public bool IsListening { get; private set; }

    /// <summary>
    ///     Fake placeholder peer
    /// </summary>

    private ISteamNetPeer FakePeer => field ??= new SteamNetPeerFake();

    /// <summary>
    ///     The first connected peer, mainly client->host
    /// </summary>
    public ISteamNetPeer FirstPeer => _peers.FirstOrDefault(p => p.IsConnected) ?? FakePeer;

    /// <summary>
    ///     The optimized broadcast peer, mainly host->multiple clients
    /// </summary>
    public ISteamNetPeer Broadcast => _broadcast;

    /// <summary>
    ///     All connected peers
    /// </summary>
    public IReadOnlyList<ISteamNetPeer> Peers => _peers.ToList();

    /// <summary>
    ///     Any connected peers
    /// </summary>
    public bool IsConnected => _peers.Any(p => p.IsConnected);

    /// <summary>
    ///     Initialize poll group and optionally steam lobby
    /// </summary>
    public void Initialize(ISteamNetListener listener)
    {
        if (_pollGroup != HSteamNetPollGroup.Invalid) {
            return;
        }

        _pollGroup = SteamNetworkingSockets.CreatePollGroup();
        _listener = listener;

        SteamCallback<SteamNetConnectionStatusChangedCallback_t>.Add(HandleStatusChange);
    }

    /// <summary>
    ///     Poll batched events on all peers
    /// </summary>
    public void Poll()
    {
        if (_pollGroup == HSteamNetPollGroup.Invalid) {
            return;
        }

        var received = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(_pollGroup, _batchedMessages, _batchedMessages.Length);

        for (var i = 0; i < received; ++i) {
            var msg = SteamNetworkingMessage_t.FromIntPtr(_batchedMessages[i]);
            try {
                var peer = _peers.Find(p => p.Connection == msg.m_conn);
                if (peer is null) {
                    continue;
                }

                var bytes = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, bytes, 0, msg.m_cbSize);

                var (typeHash, payload) = SteamNetSerializer.ExtractTypeAndPayload(bytes);
                var type = SteamNetTypeRegistry.Resolve(typeHash);
                if (type == null) {
                    EmpLog.Warning("Failed to parse type hash {TypeHash}",
                        typeHash);
                    continue;
                }

                var packet = _serializer.Deserialize(payload, type);
                _listener?.OnMessageReceived(packet, peer);

                peer.Stat.Received(msg.m_cbSize);
            } finally {
                SteamNetworkingMessage_t.Release(_batchedMessages[i]);
            }
        }
    }

#region Connection Management

    /// <summary>
    ///     Ungracefully discard the listen socket
    /// </summary>
    public void Stop()
    {
        foreach (var peer in Peers) {
            Disconnect(peer, "emp_shutdown");
        }

        DiscardListenSocket();
    }

    /// <summary>
    ///     Disconnect with reason
    /// </summary>
    public void Disconnect(ISteamNetPeer disconnectPeer, string reason)
    {
        if (disconnectPeer is not SteamNetPeer peer) {
            return;
        }

        EmpLog.Verbose("Closing connection {@Peer}",
            peer);

        SteamNetworkingSockets.SetConnectionPollGroup(peer.Connection, HSteamNetPollGroup.Invalid);

        SteamNetworkingSockets.CloseConnection(peer.Connection, 0, reason, false);

        _peers.Remove(peer);
        _broadcast.RemoveTarget(peer);

        // only call OnPeerDisconnect from steam callback
        // so we can clean up on our side without self triggering peer disconnected

        peer.Dispose();
    }

    private ISteamNetPeer AddConnection(HSteamNetConnection connection)
    {
        EmpLog.Verbose("Adding connection handle to poll group");

        if (_peers.FirstOrDefault(p => p.Connection == connection) is { } duplicate) {
            EmpLog.Verbose("Duplicate connection! Ignored");
            return duplicate;
        }

        SteamNetworkingSockets.SetConnectionPollGroup(connection, _pollGroup);

        var peer = new SteamNetPeer(connection, _serializer);

        _peers.Add(peer);
        _broadcast.AddTarget(peer);

        _listener?.OnPeerConnected(peer);

        return peer;
    }

    private void HandleStatusChange(SteamNetConnectionStatusChangedCallback_t status)
    {
        var connection = status.m_hConn;

        switch (status.m_info.m_eState) {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                if (IsHost) {
                    // we only accept as host
                    AcceptIfVersionMatch(connection, status.m_info);
                }

                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                // this should only call for host
                // but check just in case
                if (IsHost) {
                    AddConnection(connection);
                }

                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                var peer = _peers.FirstOrDefault(p => p.Connection == connection);
                if (peer is not null) {
                    _listener?.OnPeerDisconnected(peer, status.m_info.m_szEndDebug);
                    Disconnect(peer, "emp_remote_closed");
                }

                break;
        }
    }

#endregion

#region Cleanups

    ~SteamNetManager()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) {
            return;
        }

        DiscardListenSocket();

        SteamCallback<SteamNetConnectionStatusChangedCallback_t>.Remove(HandleStatusChange);

        _disposed = true;
    }

#endregion
}