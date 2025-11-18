using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SteamLobbyRequest
{
    [Key(0)]
    public ulong LobbyId { get; init; }
}