using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private static readonly Dictionary<ulong, uint> _recentPeers = [];
    private static readonly Func<int, IntPtr> _allocator = Marshal.AllocHGlobal;
    private static readonly Action<IntPtr> _deallocator = Marshal.FreeHGlobal;
    private static readonly Func<IntPtr, IntPtr, IntPtr> _reallocator = Marshal.ReAllocHGlobal;

    private static int _nextId;

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private readonly object _arenaLock = new();
    private readonly ISteamNetSerializer _serializer;

    public readonly HSteamNetConnection Connection;
    public readonly SteamNetworkingIdentity RemoteIdentity;

    private IntPtr _arena;
    private int _arenaSize;
    private bool _disposed;

    public SteamNetPeer(HSteamNetConnection connection, ISteamNetSerializer serializer)
    {
        Connection = connection;

        SteamNetworkingSockets.GetConnectionInfo(connection, out var info);
        RemoteIdentity = info.m_identityRemote;

        Uid = RemoteIdentity.GetSteamID64();
        Name = "";
        SteamUserName.PinUserName(RemoteIdentity.GetSteamID64(), name => Name = name);

        if (_recentPeers.TryGetValue(Uid, out var id)) {
            Id = id;
            Interlocked.Decrement(ref _nextId);
        } else {
            _recentPeers[Uid] = Id;
        }

        _serializer = serializer;
        _arenaSize = MemoryArenaInitialSize;
        _arena = _allocator(_arenaSize);
    }

    public uint Id { get; } = (uint)Interlocked.Increment(ref _nextId);
    public ulong Uid { get; }
    public string Name { get; private set; }

    [field: AllowNull]
    public SteamNetPeerStat Stat => field ??= new();

    public bool IsConnected =>
        Connection != HSteamNetConnection.Invalid &&
        SteamNetworkingSockets.GetConnectionInfo(Connection, out var info) &&
        info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;

    public bool Send<T>(T message, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        var bytes = _serializer.Serialize(message);
        return Send(bytes, sendFlags);
    }

    public bool Send(byte[] bytes, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        var size = bytes.Length;

        lock (_arenaLock) {
            PinArena(size);

            Marshal.Copy(bytes, 0, _arena, size);

            // crash on native side cannot be handled
            var result = SteamNetworkingSockets.SendMessageToConnection(Connection, _arena, (uint)size, (int)sendFlags, out _);
            if (result != EResult.k_EResultOK) {
                return false;
            }
        }

        Stat.Sent(size);
        UpdateRealtime();

        return true;
    }

    private void PinArena(int size)
    {
        if (size <= _arenaSize) {
            return;
        }

        var newSize = Math.Max(size, _arenaSize * MemoryArenaGrowthRatio);

        _arena = _reallocator(_arena, (IntPtr)newSize);
        _arenaSize = newSize;
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

        if (_arena != IntPtr.Zero) {
            _deallocator(_arena);
            _arena = IntPtr.Zero;
        }

        _disposed = true;
    }

#endregion
}