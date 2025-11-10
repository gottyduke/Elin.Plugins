namespace ElinTogether.Net;

public class NetPeerState
{
    public uint Index { get; init; }
    public ulong Uid { get; init; }
    public string Name { get; init; } = "";
    public bool IsValidated { get; internal init; }

    public Chara? Chara { get; init; }
}