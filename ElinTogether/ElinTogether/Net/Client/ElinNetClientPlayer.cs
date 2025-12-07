using Cwl.Helper.Unity;
using ElinTogether.Models;
using Steamworks;
using UnityEngine.Events;

namespace ElinTogether.Net;

internal partial class ElinNetClient
{
    /// <summary>
    ///     Net event: Local character creation requested
    /// </summary>
    private void OnSessionNewPlayerRequest(SessionNewPlayerRequest request)
    {
        ui.RemoveLayer<LayerEditBio>();
        var embark = ui.AddLayer<LayerEditBio>();
        var content = embark.GetComponentInChildren<Content>();

        // disable mode selection
        content.transform.GetFirstChildWithName("Mode").SetActive(false);

        // swap out the click event delegate
        var button = content.transform.GetFirstChildWithName("ButtonEmbark")!.GetComponentInChildren<UIButton>();
        button.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
        button.onClick.AddListener(() => {
            Socket.FirstPeer.Send(request.Ready());
            game.Kill();
            ui.RemoveLayer(embark);
        });
    }

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
        var find = game.cards.Find(remoteChara.uid);

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