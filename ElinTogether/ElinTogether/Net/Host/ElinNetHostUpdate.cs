using System;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    private WorldStateSnapshot? _lastTick;
    private bool _pauseUpdate;

    public WorldStateSnapshot PropagateWorldState()
    {
        return core.game?.activeZone?.map is null
            ? _lastTick!
            : WorldStateSnapshot.Create(ActiveRemoteCharas);
    }

    public void StartWorldStateUpdate()
    {
        // 5hz world snapshot reconciliation
        Scheduler.Subscribe(WorldStateSnapshotUpdate, 5);
        // 25hz delta dispatch
        Scheduler.Subscribe(WorldStateDeltaUpdate, 25);
        // 50hz delta process
        Scheduler.Subscribe(WorldStateDeltaProcess, 50);
    }

    public void StopWorldStateUpdate()
    {
        Scheduler.Unsubscribe(WorldStateSnapshotUpdate);
        Scheduler.Unsubscribe(WorldStateDeltaUpdate);
        Scheduler.Unsubscribe(WorldStateDeltaProcess);

        NetSession.Instance.Tick = 0U;

        _pauseUpdate = false;

        EmpLog.Debug("Stopping server state update");
    }

    public void PauseWorldStateUpdate()
    {
        _pauseUpdate = true;

        EmpLog.Debug("Pausing server state update");
    }

    public void ResumeWorldStateUpdate()
    {
        _pauseUpdate = false;

        EmpLog.Debug("Resuming server state update");
    }

    /// <summary>
    /// </summary>
    private void WorldStateSnapshotUpdate()
    {
        if (_pauseUpdate) {
            return;
        }

        NetSession.Instance.Tick++;

        try {
            _lastTick = PropagateWorldState();
        } catch (Exception ex) {
            EmpLog.Verbose(ex, "Exception at server tick update");
        }

        Broadcast(_lastTick);
    }

    private void WorldStateDeltaUpdate()
    {
        if (_pauseUpdate) {
            return;
        }

        if (!Delta.HasPendingOut) {
            return;
        }

        Broadcast(new WorldStateDeltaList {
            DeltaList = Delta.FlushOutBuffer(),
        });
    }

    private void WorldStateDeltaProcess()
    {
        Delta.ProcessLocalBatch(this, 16);
    }

    /// <summary>
    ///     Respond to manual requests
    /// </summary>
    private void OnWorldStateRequest(WorldStateRequest request, ISteamNetPeer peer)
    {
        peer.Send(PropagateWorldState());
    }

    /// <summary>
    ///     Apply delta changes from all clients
    /// </summary>
    private void OnWorldStateDeltaResponse(WorldStateDeltaList response, ISteamNetPeer peer)
    {
        foreach (var delta in response.DeltaList) {
            Delta.AddLocal(delta);
        }
    }
}