using Cwl.API.Attributes;
using ElinTogether.Net;
using ReflexCLI.Attributes;
using Steamworks;

namespace ElinTogether;

[ConsoleCommandClassCustomizer("emp")]
internal class EmpConsole
{
    [ConsoleCommand("add_server")]
    [CwlContextMenu("EMP/As Server")]
    internal static void AddServer()
    {
        var server = NetSession.Instance.InitializeComponent<ElinNetHost>();
        server.StartServer();
    }

    [ConsoleCommand("add_client")]
    [CwlContextMenu("EMP/As Client")]
    internal static void AddClient()
    {
        var client = NetSession.Instance.InitializeComponent<ElinNetClient>();

        var selfId = SteamUser.GetSteamID();
        var targetId = 76561198412175578UL;
        if (targetId == (ulong)selfId) {
            targetId = 76561198254677013UL;
        }

        client.ConnectSteamUser(targetId);
    }

    [ConsoleCommand("connect_steam")]
    internal static void AddClientToSteamId(ulong steamId)
    {
        var client = NetSession.Instance.InitializeComponent<ElinNetClient>();
        client.ConnectSteamUser(steamId);
    }
}