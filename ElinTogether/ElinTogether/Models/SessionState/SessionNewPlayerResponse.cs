using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Net packet: Client -> Host
/// </summary>
[MessagePackObject]
public class SessionNewPlayerResponse
{
    [Key(0)]
    public required LZ4Bytes Chara { get; init; }
    // TODO add portrait and PCC syncs
}