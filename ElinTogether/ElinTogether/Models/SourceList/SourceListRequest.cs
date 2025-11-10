using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SourceListRequest
{
    [Key(0)]
    public SourceListType Type { get; init; }
}