using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ClientCharaStateSnapshot
{
    [Key(0)]
    public required int LastAct { get; init; }

    [Key(2)]
    public required int LastReceivedTick { get; init; }

    [Key(3)]
    public required int Speed { get; init; }

    [Key(4)]
    public required RemoteCard? HeldMainHand { get; init; }

    [Key(5)]
    public required RemoteCard? HeldOffHand { get; init; }

    [Key(6)]
    public required int Dir { get; init; }
}