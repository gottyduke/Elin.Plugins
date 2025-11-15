using System;
using ElinTogether.Common;
using Serilog.Events;
using Steamworks;

namespace ElinTogether.Net.Steam;

public partial class SteamNetManager
{
    /// <summary>
    ///     Start server on valve SDR
    /// </summary>
    public void StartServerSdr()
    {
        EmpLog.Debug("Starting relay server via SDR");

        _listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 1, [_connectionKeyConfig]);
        if (_listenSocket == HSteamListenSocket.Invalid) {
            throw new InvalidOperationException("Failed to create listen socket via SDR");
        }

        SetupSteamCallback();
    }

    /// <summary>
    ///     Mainly just for debugging
    /// </summary>
    public void StartServerUdp(ushort port = EmpConstants.LocalPort)
    {
        EmpLog.Debug("Starting local udp server at port {Port}",
            port);

        var localhost = new SteamNetworkingIPAddr();
        localhost.Clear();
        localhost.m_port = port;

        _listenSocket = SteamNetworkingSockets.CreateListenSocketIP(ref localhost, 1, [_connectionKeyConfig]);
        if (_listenSocket == HSteamListenSocket.Invalid) {
            throw new InvalidOperationException("Failed to create listen socket via UDP");
        }

        SetupSteamCallback();
    }

    private void AcceptIfVersionMatch(HSteamNetConnection connection, SteamNetConnectionInfo_t info)
    {
        EmpLog.Debug("Received connection request from {RemoteIdentity}",
            info.m_identityRemote.GetSteamID64());

#if !DEBUG
        if (info.m_nUserData != _connectionKey) {
            EmpPop.Debug("Rejected connection request from {SteamIdentity}\nBuildVersions mismatch\n" +
                         "Host version is {HostVersion}\nClient version is {ClientVersion}",
                info.m_identityRemote.GetSteamID64(), ModInfo.BuildVersion.TagColor(Color.green),
                BuildVersionIntegrity.LongToVersionString(info.m_nUserData).TagColor(Color.red));

            // only connect if we have same build version
            SteamNetworkingSockets.CloseConnection(connection, 0, "emp_version_mismatch", false);
        } else
#endif
        {
            EmpLog.Debug("Accepting connection request from {RemoteIdentity}",
                info.m_identityRemote.GetSteamID64());

            var result = SteamNetworkingSockets.AcceptConnection(connection);
            if (result != EResult.k_EResultOK) {
                EmpPop.Popup(LogEventLevel.Warning, "Failed to accept connection");
            }
        }
    }

    private void DiscardListenSocket()
    {
        if (_listenSocket != HSteamListenSocket.Invalid) {
            SteamNetworkingSockets.CloseListenSocket(_listenSocket);
            _listenSocket = HSteamListenSocket.Invalid;
        }

        IsHost = false;
        IsListening = false;
    }

    private void SetupSteamCallback()
    {
        if (IsListening) {
            return;
        }

        IsHost = true;
        IsListening = true;
    }
}