using ElinTogether.Models;
using Steamworks;

namespace ElinTogether.Net;

internal partial class ElinNetClient
{
    /// <summary>
    ///     Net event: Save probe received after connection
    /// </summary>
    /// <param name="probe"></param>
    private void OnSaveDataProbe(SaveDataProbe probe)
    {
        EmpLog.Debug("Received save data from host");

        var probeGame = probe.Game.Decompress<Game>();

        core.game = probeGame;
        Game.id = "world_emp";

        var remoteChara = Session.Player = probe.Chara.Decompress<Chara>();

        player.uidChara = remoteChara.uid;
        player.chara = remoteChara;

        probeGame.isCloud = false;
        probeGame.isLoading = true;
        probeGame.OnGameInstantiated();
        probeGame.OnLoad();

        ui.RemoveLayer<LayerTitle>();
        ui.ShowCover();

        player.zone = null;

        // do an initial zone request to load in
        RequestZoneState(MapDataRequest.CurrentRemoteZone);

        EmpPop.Debug("Waiting on zone state complete...");

        probeGame.isLoading = false;
    }

    /// <summary>
    ///     Net event: Join steam lobby if not already in it
    /// </summary>
    private void OnSteamLobbyRequest(SteamLobbyRequest request)
    {
        if (Session.Lobby.Current?.LobbyId != (CSteamID)request.LobbyId) {
            Session.Lobby.ConnectLobby(request.LobbyId);
        }
    }
}