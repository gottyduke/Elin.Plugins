using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateRequest
{
    [Key(0)]
    public uint ServerTick { get; init; }
}