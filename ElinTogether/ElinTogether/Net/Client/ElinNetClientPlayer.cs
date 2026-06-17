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
        EmpLog.Information("Received new player creation request");

        ui.RemoveLayer<LayerEditBio>();
        var embark = ui.AddLayer<LayerEditBio>();
        var content = embark.GetComponentInChildren<Content>();

        // disable mode selection
        content.transform.Find("Mode").SetActive(false);

        // swap out the click event delegate
        var button = content.transform.Find("ButtonEmbark")!.GetComponentInChildren<UIButton>();
        button.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
        button.onClick.AddListener(() => {
            Host.Send(request.Ready());
            game.Kill();
            ui.RemoveLayer(embark);
            core.game = null;
        });
    }

    /// <summary>
    ///     Net event: Save probe received after connection.
    /// </summary>
    private void OnSaveDataProbe(SaveDataProbe probe)
    {
        EmpLog.Information("Received save data from host");

        var probeGame = probe.MakeGameSave();

        core.game = probeGame;
        Game.id = "world_emp";

        var remoteChara = Session.Player = game.cards.globalCharas.Find(probe.RemoteCharaUid);

        player.uidChara = remoteChara.uid;
        player.chara = remoteChara;

        var hostSteamId = Session.LastSession?.HostSteamId ?? Host?.Uid ?? 0;
        Session.LastSession = new() {
            HostSteamId = hostSteamId,
            SessionId = Session.SessionId,
            CharaUid = remoteChara.uid,
            LastServerTick = Session.Tick,
            LastZoneUid = Session.CurrentZone?.uid,
            LastZoneFullName = Session.CurrentZone?.ZoneFullName,
        };

        probeGame.isCloud = false;
        probeGame.isLoading = true;
        probeGame.OnGameInstantiated();
        probeGame.OnLoad();

        ui.RemoveLayer<LayerTitle>();
        ui.ShowCover();
        //scene.Init(Scene.Mode.StartGame);
        player.zone = null;
        core.actionsNextFrame.Add(LayerTitle.KillActor);

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

    private void OnSessionRejoinResponse(SessionRejoinResponse response)
    {
        if (!response.Success) {
            EmpLog.Warning("Rejoin rejected by host: {Reason}", response.Reason);
            // fall back as non-recoverable
            Session.RemoveComponent();
            if (core.IsGameStarted) {
                scene.Init(Scene.Mode.Title);
            }
            return;
        }

        if (Session.LastSession is { } last) {
            Session.LastSession = last with {
                LastServerTick = response.CurrentServerTick,
                LastZoneUid = response.CurrentZoneUid,
                LastZoneFullName = response.CurrentZoneFullName,
            };
        }

        Session.SetPhase(ConnectionPhase.Synchronized);
        EmpLog.Information("Rejoin successful. Resuming at tick {Tick}", response.CurrentServerTick);

        var current = Session.CurrentZone;
        if (response.CurrentZoneUid != null &&
            (response.CurrentZoneUid != current?.uid || response.CurrentZoneFullName != current.ZoneFullName)) {
            EmpLog.Debug("Rejoin zone mismatch detected, requesting zone {ZoneUid} {ZoneFullName}",
                response.CurrentZoneUid, response.CurrentZoneFullName);
            RequestZoneState(new() {
                ZoneUid = (int)response.CurrentZoneUid,
                ZoneFullName = response.CurrentZoneFullName ?? "",
            });
        }
    }
}