using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SessionRejoinRequest
{
    [Key(0)]
    public int LastKnownServerTick { get; init; }

    [Key(1)]
    public int CharaUid { get; init; }

    [Key(2)]
    public int? LastZoneUid { get; init; }
}