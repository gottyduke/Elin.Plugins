namespace ElinTogether.Net.Steam;

public interface ISteamNetPeer
{
    public int Id { get; }
    public ulong Uid { get; }
    public string? Name { get; }
    public bool IsConnected { get; }
    public SteamNetPeerStat Stat { get; }
    public bool Send(byte[] bytes, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable);
    public bool Send<T>(T packet, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable);
}