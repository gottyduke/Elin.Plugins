using Cwl.API.Attributes;
using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Net.Steam;
using ReflexCLI.Attributes;
using Steamworks;

namespace ElinTogether;

[ConsoleCommandClassCustomizer("emp")]
internal class EmpConsole
{
    [ConsoleCommand("add_server")]
    internal static void AddServer()
    {
        var server = NetSession.Instance.InitializeComponent<ElinNetHost>();
        server.StartServer();
    }

    [ConsoleCommand("disconnect")]
    internal static void Disconnect()
    {
        NetSession.Instance.RemoveComponent();
    }

    [ConsoleCommand("d1")]
    internal static void AddClientD1()
    {
        AddClientToSteamId(76561198412175578UL);
    }

    [ConsoleCommand("d2")]
    internal static void AddClientD2()
    {
        AddClientToSteamId(76561198254677013UL);
    }

    [ConsoleCommand("connect_steam")]
    internal static void AddClientToSteamId(ulong steamId64)
    {
        var client = NetSession.Instance.InitializeComponent<ElinNetClient>();
        client.ConnectSteamUser(steamId64);
    }

    [ConsoleCommand("lobby.create_public")]
    internal static void CreatePublicLobby(int maxPlayers = 16)
    {
        SteamNetLobbyManager.Instance.CreateLobby(SteamNetLobbyType.Public, maxPlayers);
    }

    [ConsoleCommand("lobby.invite_steam")]
    internal static void InviteSteamUser(ulong steamId64)
    {
        SteamNetLobbyManager.Instance.InviteSteamUser(steamId64);
    }

    [ConsoleCommand("lobby.invite_overlay")]
    internal static void InviteSteamOverlay()
    {
        SteamNetLobbyManager.Instance.InviteSteamOverlay();
    }
}