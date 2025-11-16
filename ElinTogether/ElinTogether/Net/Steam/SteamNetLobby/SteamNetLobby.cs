using Steamworks;

namespace ElinTogether.Net.Steam;

public class SteamNetLobby(CSteamID steamLobbyId)
{
    public readonly CSteamID LobbyId = steamLobbyId;
    public CSteamID OwnerId { get; private set; } = CSteamID.Nil;
    public string EmpVersion { get; private set; } = "";
    public string OwnerName { get; private set; } = "";
    public string GameVersion { get; private set; } = "";
    public string CurrentZone { get; private set; } = "";
    public int PlayerCount { get; private set; }

    public void RefreshData()
    {
        OwnerId = GetLobbyOwner();
        EmpVersion = GetLobbyData("EmpVersion");
        OwnerName = GetLobbyData("OwnerName");
        GameVersion = GetLobbyData("GameVersion");
        CurrentZone = GetLobbyData("CurrentZone");
        PlayerCount = GetCurrentPlayersCount();
    }

    public void SetLobbyData(string key, string value)
    {
        SteamMatchmaking.SetLobbyData(LobbyId, key, value);
    }

    public string GetLobbyData(string key)
    {
        return SteamMatchmaking.GetLobbyData(LobbyId, key);
    }

    public CSteamID GetLobbyOwner()
    {
        return SteamMatchmaking.GetLobbyOwner(LobbyId);
    }

    public int GetCurrentPlayersCount()
    {
        return SteamMatchmaking.GetNumLobbyMembers(LobbyId);
    }
}