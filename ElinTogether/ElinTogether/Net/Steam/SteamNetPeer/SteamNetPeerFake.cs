using System.Diagnostics.CodeAnalysis;

namespace ElinTogether.Net.Steam;

internal class SteamNetPeerFake : ISteamNetPeer
{
    public int Id => -1;
    public ulong Uid => unchecked((ulong)-1);
    public string Name => "emp-no-connection";
    public bool IsConnected => true;

    [field: AllowNull]
    public SteamNetPeerStat Stat => field ??= new();

    public bool Send(byte[] bytes, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        return true;
    }

    public bool Send<T>(T packet, SteamNetSendFlag sendFlags = SteamNetSendFlag.Reliable)
    {
        return true;
    }
}