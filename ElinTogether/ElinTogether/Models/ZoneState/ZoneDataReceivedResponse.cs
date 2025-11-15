using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ZoneDataReceivedResponse
{
    [Key(0)]
    public required int ZoneUid { get; init; }
}