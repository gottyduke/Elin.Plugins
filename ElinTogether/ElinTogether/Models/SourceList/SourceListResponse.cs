using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SourceListResponse
{
    [Key(0)]
    public SourceListType Type { get; init; }

    [Key(1)]
    public byte[] Checksum { get; set; } = [];
}