using System;
using System.Collections.Generic;
using Cwl.Helper.Exceptions;

namespace ElinTogether.Net.Steam;

public sealed class SteamNetMessageRouter : ISteamNetListener
{
    private readonly Dictionary<uint, Action<object, ISteamNetPeer>> _handlers = [];

    public void OnPeerConnected(ISteamNetPeer peer)
    {
        OnPeerConnectedEvent?.Invoke(peer);
    }

    public void OnPeerDisconnected(ISteamNetPeer peer, string reason)
    {
        OnPeerDisconnectedEvent?.Invoke(peer, reason.lang());
    }

    public void OnMessageReceived(object msg, ISteamNetPeer peer)
    {
        if (_handlers.TryGetValue(SteamNetTypeRegistry.GetHash(msg.GetType()), out var handler)) {
            handler(msg, peer);
        }
    }

    public event Action<ISteamNetPeer>? OnPeerConnectedEvent;
    public event Action<ISteamNetPeer, string>? OnPeerDisconnectedEvent;

    /// <summary>
    ///     Register with data only
    /// </summary>
    public void RegisterHandler<T>(Action<T> handler)
    {
        _handlers[SteamNetTypeRegistry.GetHash<T>()] = SafeInvokeT1;

        return;

        void SafeInvokeT1(object packet, ISteamNetPeer peer)
        {
            try {
                handler((T)packet);
            } catch (Exception ex) {
                EmpLog.Verbose(ex, "Exception at handling T1 message {CallbackName}, {MessageType}",
                    handler.Method.Name, typeof(T).Name);
                DebugThrow.Void(ex);
                // noexcept
            }
        }
    }

    /// <summary>
    ///     Register with remote peer as input
    /// </summary>
    public void RegisterHandler<T>(Action<T, ISteamNetPeer> handler)
    {
        _handlers[SteamNetTypeRegistry.GetHash<T>()] = SafeInvokeT2;

        return;

        void SafeInvokeT2(object packet, ISteamNetPeer peer)
        {
            try {
                handler((T)packet, peer);
            } catch (Exception ex) {
                EmpLog.Verbose(ex, "Exception at handling T2 message {CallbackName}, {MessageType}, from {@Peer}",
                    handler.Method.Name, typeof(T).Name, peer);
                DebugThrow.Void(ex);
                // noexcept
            }
        }
    }
}