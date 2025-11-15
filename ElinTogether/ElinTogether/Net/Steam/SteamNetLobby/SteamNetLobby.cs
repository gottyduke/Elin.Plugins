using System;
using Steamworks;

namespace ElinTogether.Net.Steam;

public class SteamNetLobby(CSteamID steamLobbyId)
{
    public readonly CSteamID LobbyId = steamLobbyId;
    public CSteamID OwnerId = CSteamID.Nil;
    public string OwnerName = "";
    public string EmpVersion = "";

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