using Cwl.API.Attributes;
using ElinTogether.Net;
using ReflexCLI.Attributes;

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
    internal static void AddClientToSteamId(ulong steamId)
    {
        var client = NetSession.Instance.InitializeComponent<ElinNetClient>();
        client.ConnectSteamUser(steamId);
    }
}