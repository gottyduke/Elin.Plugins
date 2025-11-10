using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SourceDiffResponse
{
    [Key(0)]
    public required SourceListType Type { get; init; }

    [Key(1)]
    public required string[] IdList { get; init; }
}