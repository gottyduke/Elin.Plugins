using MessagePack;

namespace ElinTogether.Net;

[MessagePackObject]
public class NetPeerState
{
    [Key(0)]
    public required uint Index { get; init; }

    [Key(1)]
    public required ulong Uid { get; init; }

    [Key(2)]
    public required string Name { get; init; }

    [Key(3)]
    public required int CharaUid { get; init; }

    [Key(4)]
    public bool IsValidated { get; internal init; }

    [Key(5)]
    public byte LastAct { get; set; }

    [Key(6)]
    public int Speed { get; set; }
}