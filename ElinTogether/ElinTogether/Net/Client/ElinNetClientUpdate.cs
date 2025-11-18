using ElinTogether.Models;

namespace ElinTogether.Net;

internal partial class ElinNetClient
{
    private WorldStateSnapshot? _lastTick;
    private bool _pauseUpdate;

    /// <summary>
    ///     Send out local deltas and self snapshot to remote host
    /// </summary>
    private void WorldStateDeltaUpdate()
    {
        if (_pauseUpdate) {
            return;
        }

        // send out delta
        if (!Delta.HasPendingOut) {
            return;
        }

        Socket.FirstPeer.Send(new WorldStateDeltaList {
            DeltaList = Delta.FlushOutBuffer(),
        });
    }

    /// <summary>
    ///     Process local or deferred deltas received from host
    /// </summary>
    private void WorldStateDeltaProcess()
    {
        if (!Delta.HasPendingIn) {
            return;
        }

        Delta.ProcessLocalBatch(this, 16);
    }

    /// <summary>
    ///     Net event: Apply client-side reconciliation from host
    /// </summary>
    private void OnWorldStateSnapshot(WorldStateSnapshot snapshot)
    {
        if (_lastTick is not null) {
            var dropped = snapshot.ServerTick - _lastTick.ServerTick;
            if (dropped > 1) {
                EmpLog.Warning("Falling behind with {DroppedTicks} dropped ticks",
                    dropped);
            }
        }

        _lastTick = snapshot;
        Session.Tick = snapshot.ServerTick;

        if (core.game?.activeZone?.map is null) {
            return;
        }

        // apply world state
        snapshot.ApplyReconciliation();

        // send back client state
        Socket.FirstPeer.Send(CharaStateSnapshot.CreateSelf());
    }

    /// <summary>
    ///     Net event: Apply delta changes from host
    /// </summary>
    private void OnWorldStateDeltaResponse(WorldStateDeltaList response)
    {
        foreach (var delta in response.DeltaList) {
            Delta.AddLocal(delta);
        }
    }

    /// <summary>
    ///     Net event: Apply new session remote player states
    /// </summary>
    private void OnSessionStatesUpdate(SessionPlayersSnapshot states)
    {
        states.Apply();
    }

#region Scheduler Jobs

    /// <summary>
    ///     Subscribe all scheduler jobs and reset pause state
    /// </summary>
    public void StartWorldStateUpdate()
    {
        // 25hz delta dispatch
        Scheduler.Subscribe(WorldStateDeltaUpdate, 25f);
        // 50hz delta process
        Scheduler.Subscribe(WorldStateDeltaProcess, 50f);

        _pauseUpdate = false;
    }

    /// <summary>
    ///     Unsubscribe all scheduler jobs and reset pause state
    /// </summary>
    public void StopWorldStateUpdate()
    {
        Scheduler.Unsubscribe(WorldStateDeltaUpdate);
        Scheduler.Unsubscribe(WorldStateDeltaProcess);

        _pauseUpdate = false;

        EmpLog.Debug("Stopping client state update");
    }

    /// <summary>
    ///     Pause sending out deltas, *but they still accumulate*
    /// </summary>
    public void PauseWorldStateUpdate()
    {
        _pauseUpdate = true;

        EmpLog.Debug("Pausing client state update");
    }

    /// <summary>
    ///     Resume sending out deltas
    /// </summary>
    public void ResumeWorldStateUpdate(bool clearDelta = false)
    {
        _pauseUpdate = false;

        EmpLog.Debug("Resuming client state update");

        if (clearDelta) {
            Delta.ClearOut();
        }
    }

#endregion
}