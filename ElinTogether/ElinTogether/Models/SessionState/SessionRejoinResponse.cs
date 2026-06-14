using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SessionRejoinResponse
{
    [Key(0)]
    public bool Success { get; init; }

    [Key(1)]
    public int CurrentServerTick { get; init; }

    [Key(2)]
    public int? CurrentZoneUid { get; init; }

    [Key(3)]
    public string? CurrentZoneFullName { get; init; }

    [Key(4)]
    public string? Reason { get; init; }
}