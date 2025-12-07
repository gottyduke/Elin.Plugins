using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Net packet: Host -> Client
/// </summary>
[MessagePackObject]
public class SteamLobbyRequest
{
    [Key(0)]
    public ulong LobbyId { get; init; }
}