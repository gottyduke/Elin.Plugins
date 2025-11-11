using Cwl.Helper.Unity;
using ElinTogether.Models;

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

        var remoteChara = NetSession.Instance.Player = probe.Chara.Decompress<Chara>();

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
        RequestZoneState();

        EmpLog.Debug("Waiting on zone state complete...");

        this.StartDeferredCoroutine(
            () => {
                EmpLog.Debug("Starting initial scene init");
                player.zone = pc.currentZone = NetSession.Instance.CurrentZone;
                scene.Init(Scene.Mode.Zone);
            },
            () => NetSession.Instance.CurrentZone is not null);

        probeGame.isLoading = false;
    }
}