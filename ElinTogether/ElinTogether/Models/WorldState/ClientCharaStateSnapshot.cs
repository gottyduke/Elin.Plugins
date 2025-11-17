using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ClientCharaStateSnapshot
{
    [Key(0)]
    public int LastAct { get; init; }

    [Key(2)]
    public int LastReceivedTick { get; init; }

    [Key(3)]
    public int Speed { get; init; }

    [Key(4)]
    public RemoteCard? HeldMainHand { get; init; }

    [Key(5)]
    public RemoteCard? HeldOffHand { get; init; }

    [Key(6)]
    public int Dir { get; init; }
}