using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using ElinTogether.Helper;
using Steamworks;
using UnityEngine;

namespace ElinTogether.Net.Steam;

internal class SteamNetPeer : ISteamNetPeer, IDisposable
{
    private const int MemoryArenaInitialSize = 4 * (1 << 10);
    private const int MemoryArenaGrowthRatio = 2;

    private static readonly ConcurrentDictionary<ulong, int> _recentPeers = [];
    private static readonly Func<int, IntPtr> _allocator = Marshal.AllocHGlobal;
    private static readonly Action<IntPtr> _deallocator = Marshal.FreeHGlobal;
    private static readonly Func<IntPtr, IntPtr, IntPtr> _reallocator = Marshal.ReAllocHGlobal;

    private static int _nextId = -1;

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    protected readonly object ArenaLock = new();

    public readonly HSteamNetConnection Connection;
    public readonly SteamNetworkingIdentity RemoteIdentity;
    protected readonly ISteamNetSerializer Serializer;
    private bool _disposed;

    protected IntPtr Arena;
    protected int ArenaSize;

    public SteamNetPeer(HSteamNetConnection connection, ISteamNetSerializer serializer)
    {
        Connection = connection;

        SteamNetworkingSockets.GetConnectionInfo(connection, out var info);
        RemoteIdentity = info.m_identityRemote;

        Uid = RemoteIdentity.GetSteamID64();
        SteamUserName.PinUserName(RemoteIdentity.GetSteamID64(), name => Name = name);

        // use recent Id if it's a reconnection
        if (!_recentPeers.TryGetValue(Uid, out var id)) {
            _recentPeers[Uid] = id = Interlocked.Increment(ref _nextId);
        }

        Id = id;

        Serializer = serializer;
        ArenaSize = MemoryArenaInitialSize;
        Arena = _allocator(ArenaSize);
    }

    public ESteamNetworkingConnectionState ConnectionState =>
        SteamNetworkingSockets.GetConnectionInfo(Connection, out var info)
            ? info.m_eState
            : ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None;

    public virtual int Id { get; }
    public ulong Uid { get; }
    public virtual string? Name { get; private set; }


    public SteamNetPeerStat Stat => field ??= new();

    public virtual bool IsConnected =>
        ConnectionState is
            ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting or
            ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute or
            ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;

    public virtual bool Send<T>(T message, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        var bytes = Serializer.Serialize(message);
        return Send(bytes, sendFlags);
    }

    public virtual bool Send(byte[] bytes, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        var size = bytes.Length;

        lock (ArenaLock) {
            PinArena(size);

            Marshal.Copy(bytes, 0, Arena, size);

            // crash on native side cannot be handled
            var result = SteamNetworkingSockets.SendMessageToConnection(Connection, Arena, (uint)size, (int)sendFlags, out _);
            if (result != EResult.k_EResultOK) {
                return false;
            }
        }

        Stat.Sent(size);
        UpdateRealtime();

        return true;
    }

    protected void PinArena(int size)
    {
        if (size <= ArenaSize) {
            return;
        }

        var newSize = Math.Max(size, ArenaSize * MemoryArenaGrowthRatio);

        Arena = _reallocator(Arena, (IntPtr)newSize);
        ArenaSize = newSize;
    }

    public void UpdateRealtime()
    {
        const float pingAlpha = 0.2f;
        const float bandwidthAlpha = 0.15f;

        var status = new SteamNetConnectionRealTimeStatus_t();
        var discard = new SteamNetConnectionRealTimeLaneStatus_t();
        var result = SteamNetworkingSockets.GetConnectionRealTimeStatus(Connection, ref status, 0, ref discard);
        if (result != EResult.k_EResultOK) {
            return;
        }

        Stat.LastPingMs = status.m_nPing;
        Stat.ConnectionQualityLocal = status.m_flConnectionQualityLocal;
        Stat.ConnectionQualityRemote = status.m_flConnectionQualityRemote;
        Stat.LastUpdated = DateTime.UtcNow;

        // use ema to smooth out the spikes
        Stat.AvgPingMs = Stat.AvgPingMs == 0
            ? Stat.LastPingMs
            : Mathf.Lerp(Stat.AvgPingMs, Stat.LastPingMs, pingAlpha);

        Stat.AvgBpsOut = Stat.AvgBpsOut == 0
            ? status.m_flOutBytesPerSec
            : Mathf.Lerp(Stat.AvgBpsOut, status.m_flOutBytesPerSec, bandwidthAlpha);

        Stat.AvgBpsIn = Stat.AvgBpsIn == 0
            ? status.m_flInBytesPerSec
            : Mathf.Lerp(Stat.AvgBpsIn, status.m_flInBytesPerSec, bandwidthAlpha);
    }

#region Cleanups

    ~SteamNetPeer()
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

        if (Arena != IntPtr.Zero) {
            _deallocator(Arena);
            Arena = IntPtr.Zero;
        }

        _disposed = true;
    }

#endregion
}