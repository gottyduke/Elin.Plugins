using ElinTogether.Models;

namespace ElinTogether.Net;

internal partial class ElinNetClient
{
    private WorldStateSnapshot? _lastTick;
    private bool _pauseUpdate;

    public void StartWorldStateUpdate()
    {
        // 25hz delta dispatch
        Scheduler.Subscribe(WorldStateDeltaUpdate, 25);
        // 50hz delta process
        Scheduler.Subscribe(WorldStateDeltaProcess, 50);
    }

    public void StopWorldStateUpdate()
    {
        Scheduler.Unsubscribe(WorldStateDeltaUpdate);
        Scheduler.Unsubscribe(WorldStateDeltaProcess);

        _pauseUpdate = false;

        EmpLog.Debug("Stopping client state update");
    }

    public void PauseWorldStateUpdate()
    {
        _pauseUpdate = true;

        EmpLog.Debug("Pausing client state update");
    }

    public void ResumeWorldStateUpdate()
    {
        _pauseUpdate = false;

        EmpLog.Debug("Resuming client state update");
    }

    private void WorldStateDeltaUpdate()
    {
        if (_pauseUpdate) {
            return;
        }

        if (!Delta.HasPendingOut) {
            return;
        }

        Socket.FirstPeer.Send(new WorldStateDeltaList {
            DeltaList = Delta.FlushOutBuffer(),
        });
    }

    private void WorldStateDeltaProcess()
    {
        if (!Delta.HasPendingIn) {
            return;
        }

        Delta.ProcessLocalBatch(this, 16);
    }

    private void OnWorldStateSnapshot(WorldStateSnapshot snapshot)
    {
        if (_lastTick is not null) {
            var dropped = snapshot.ServerTick - _lastTick.ServerTick;
            if (dropped > 1) {
                EmpLog.Warning("Falling behind on server update with {DroppedTicks} dropped ticks",
                    dropped);
            }
        }

        _lastTick = snapshot;
        NetSession.Instance.Tick = snapshot.ServerTick;

        if (core.game?.activeZone?.map is null) {
            return;
        }

        // 1
        world.date.raw = snapshot.GameDate;

        // 2
        foreach (var chara in snapshot.CharaSnapshots) {
            chara.ApplyReconciliation();
        }

        // 3
        game.cards.uidNext = snapshot.GlobalUidNext;
    }

    /// <summary>
    ///     Apply delta changes from host
    /// </summary>
    private void OnWorldStateDeltaResponse(WorldStateDeltaList response)
    {
        foreach (var delta in response.DeltaList) {
            Delta.AddLocal(delta);
        }
    }
}