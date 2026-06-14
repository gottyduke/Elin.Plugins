using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Lightweight snapshot of the last known host/session used for rejoin after transient disconnect.
///     Stored on NetSession so client can attempt to resume without full SaveDataProbe.
/// </summary>
[MessagePackObject]
public record class LastSessionInfo
{
    [Key(0)]
    public ulong HostSteamId { get; init; }

    [Key(1)]
    public ulong SessionId { get; init; }

    [Key(2)]
    public int CharaUid { get; init; }

    [Key(3)]
    public int LastServerTick { get; init; }

    [Key(4)]
    public int? LastZoneUid { get; init; }

    [Key(5)]
    public string? LastZoneFullName { get; init; }
}