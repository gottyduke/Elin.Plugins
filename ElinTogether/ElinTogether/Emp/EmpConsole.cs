using ElinTogether.Net;
using ElinTogether.Net.Steam;
using ReflexCLI.Attributes;

namespace ElinTogether;

[ConsoleCommandClassCustomizer("emp")]
internal class EmpConsole
{
    [ConsoleCommand("add_local")]
    internal static void AddLocalServerUdp()
    {
        var server = NetSession.Instance.InitializeComponent<ElinNetHost>();
        server.StartServer(true);
    }

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

    [ConsoleCommand("connect_udp")]
    internal static void AddClientToUdpPort()
    {
        var client = NetSession.Instance.InitializeComponent<ElinNetClient>();
        client.ConnectLocalPort();
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
        NetSession.Instance.Lobby.CreateLobby(SteamNetLobbyType.Public, maxPlayers);
    }

    [ConsoleCommand("lobby.invite_steam")]
    internal static void InviteSteamUser(ulong steamId64)
    {
        NetSession.Instance.Lobby.InviteSteamUser(steamId64);
    }

    [ConsoleCommand("lobby.invite_overlay")]
    internal static void InviteSteamOverlay()
    {
        NetSession.Instance.Lobby.InviteSteamOverlay();
    }
}