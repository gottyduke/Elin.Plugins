namespace ElinTogether.Net.Steam;

public interface ISteamNetListener
{
    public void OnPeerConnected(ISteamNetPeer peer);
    public void OnPeerDisconnected(ISteamNetPeer peer, string reason);
    public void OnMessageReceived(object message, ISteamNetPeer peer);
}