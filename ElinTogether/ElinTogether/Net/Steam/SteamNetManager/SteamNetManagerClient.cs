using ElinTogether.Common;
using Steamworks;

namespace ElinTogether.Net.Steam;

public partial class SteamNetManager
{
    /// <summary>
    ///     Connect by steam ID using valve SDR
    /// </summary>
    public void Connect(CSteamID steamID)
    {
        EmpLog.Debug("Connecting by steam id {RemoteIdentity}",
            (ulong)steamID);

        var identity = new SteamNetworkingIdentity();
        identity.SetSteamID(steamID);

        var connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 1, [_connectionKeyConfig]);
        if (connection != HSteamNetConnection.Invalid) {
            AddConnection(connection);
        }
    }

    /// <summary>
    ///     Connect by IP address, probably port forwarding or Hamachi <br />
    ///     Do people still use hamachi?
    /// </summary>
    public void Connect(ref SteamNetworkingIPAddr address)
    {
        address.ToString(out var exposed, true);
        EmpLog.Debug("Connecting by IP {RemoteIdentity}",
            exposed.RedactedIp);

        var connection = SteamNetworkingSockets.ConnectByIPAddress(ref address, 1, [_connectionKeyConfig]);
        if (connection != HSteamNetConnection.Invalid) {
            AddConnection(connection);
        }
    }

    /// <summary>
    ///     Connect to localhost for debugging
    /// </summary>
    public void Connect(ushort port = EmpConstants.LocalPort)
    {
        var localhost = new SteamNetworkingIPAddr();
        localhost.Clear();
        localhost.m_port = port;
        //localhost.ParseString($"127.0.0.1:{port}");

        Connect(ref localhost);
    }

    /// <summary>
    ///     By ipv4 or ipv6 mapped address
    /// </summary>
    public void Connect(string address)
    {
        var endpoint = new SteamNetworkingIPAddr();
        endpoint.ParseString(address);

        Connect(ref endpoint);
    }
}