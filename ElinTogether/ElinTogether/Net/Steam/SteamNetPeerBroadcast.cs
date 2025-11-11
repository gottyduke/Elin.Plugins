using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Steamworks;

namespace ElinTogether.Net.Steam;

internal class SteamNetPeerBroadcast(ISteamNetSerializer serializer) : ISteamNetPeer, IDisposable
{
    private readonly List<SteamNetPeer> _targets = [];

    private GCHandle? _arena;
    private int _arenaSize;
    private bool _disposed;

    public uint Id => 0;
    public ulong Uid => 0;
    public string Name => "Broadcast";

    /// <summary>
    ///     Aggregated peer stats
    /// </summary>
    [field: AllowNull]
    public SteamNetPeerStat Stat => field ??= new();

    public bool IsConnected => _targets.Any(p => p.IsConnected);

    public bool Send<T>(T packet, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        var bytes = serializer.Serialize(packet);
        return Send(bytes, sendFlags);
    }

    public bool Send(byte[] bytes, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        switch (_targets.Count) {
            case 0:
                return false;
            case 1:
                return _targets[0].Send(bytes, sendFlags);
        }

        _arena?.Free();
        _arena = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var buffer = _arena.Value.AddrOfPinnedObject();
        _arenaSize = bytes.Length;

        var success = true;
        try {
            foreach (var peer in _targets) {
                if (!peer.IsConnected) {
                    continue;
                }

                var result = SteamNetworkingSockets.SendMessageToConnection(peer.Connection, buffer, (uint)_arenaSize,
                    (int)sendFlags, out _);
                if (result != EResult.k_EResultOK) {
                    success = false;
                    continue;
                }

                peer.Stat.Sent(_arenaSize);
                peer.UpdateRealtime();
            }
        } finally {
            _arena?.Free();
            _arena = null;
        }

        if (!success) {
            return success;
        }

        Stat.Sent(_arenaSize * _targets.Count);
        Stat.LastUpdated = DateTime.UtcNow;

        return success;
    }

    public void AddTarget(SteamNetPeer peer)
    {
        if (!_targets.Contains(peer)) {
            _targets.Add(peer);
        }
    }

    public void RemoveTarget(SteamNetPeer peer)
    {
        _targets.Remove(peer);
    }

    public void Clear()
    {
        _targets.Clear();
    }

#region Cleanups

    ~SteamNetPeerBroadcast()
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

        if (_arena is { IsAllocated: true }) {
            _arena.Value.Free();
        }

        _arena = null;
        _disposed = true;
    }

#endregion
}