using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ZoneDataResponse
{
    [Key(0)]
    public required MapDataResponse Map { get; init; }

    [Key(1)]
    public required LZ4Bytes Zone { get; init; }
}