using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ZoneActivateResponse
{
    [Key(0)]
    public required int ZoneUid { get; init; }

    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public string? ZoneFullName { get; init; }
}