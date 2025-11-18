using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Steamworks;

namespace ElinTogether.Net.Steam;

internal sealed class SteamNetPeerBroadcast(ISteamNetSerializer serializer)
    : SteamNetPeer(HSteamNetConnection.Invalid, serializer)
{
    private readonly List<SteamNetPeer> _targets = [];

    public override int Id => -1;
    public override string Name => "emp-broadcast";
    public override bool IsConnected => _targets.Any(p => p.IsConnected);

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

    public void ClearTargets()
    {
        _targets.Clear();
    }

    public override bool Send(byte[] bytes, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        switch (_targets.Count) {
            case 0:
                return false;
            case 1:
                return _targets[0].Send(bytes, sendFlags);
        }

        lock (ArenaLock) {
            PinArena(bytes.Length);
            Marshal.Copy(bytes, 0, Arena, bytes.Length);

            var success = true;
            foreach (var peer in _targets) {
                if (!peer.IsConnected) {
                    continue;
                }

                var result = SteamNetworkingSockets.SendMessageToConnection(peer.Connection, Arena, (uint)bytes.Length,
                    (int)sendFlags, out _);
                if (result != EResult.k_EResultOK) {
                    success = false;
                }

                peer.Stat.Sent(bytes.Length);
                peer.UpdateRealtime();
            }

            if (!success) {
                return success;
            }

            Stat.Sent(bytes.Length * _targets.Count);
            Stat.LastUpdated = DateTime.UtcNow;

            return success;
        }
    }
}