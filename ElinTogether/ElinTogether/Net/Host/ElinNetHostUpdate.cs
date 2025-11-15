using System;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    private WorldStateSnapshot? _lastTick;
    private bool _pauseUpdate;

    /// <summary>
    ///     Propagate current world state snapshot
    /// </summary>
    public WorldStateSnapshot PropagateWorldState()
    {
        return core.game?.activeZone?.map is null
            ? _lastTick!
            : WorldStateSnapshot.Create();
    }

    /// <summary>
    ///     Send out world state for client-side reconciliation
    /// </summary>
    private void WorldStateSnapshotUpdate()
    {
        if (_pauseUpdate) {
            return;
        }

        NetSession.Instance.Tick++;
        NetSession.Instance.CurrentZone = _zone;

        try {
            _lastTick = PropagateWorldState();
        } catch (Exception ex) {
            EmpLog.Verbose(ex, "Exception at server tick update");
        }

        Broadcast(_lastTick);
    }

    /// <summary>
    ///     Send out local deltas to remote clients
    /// </summary>
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

    /// <summary>
    ///     Process local or deferred deltas received from clients
    /// </summary>
    private void WorldStateDeltaProcess()
    {
        if (!Delta.HasPendingIn) {
            return;
        }

        Delta.ProcessLocalBatch(this, 16);
    }

    /// <summary>
    ///     Net event: Respond to manual requests
    /// </summary>
    private void OnWorldStateRequest(WorldStateRequest request, ISteamNetPeer peer)
    {
        peer.Send(PropagateWorldState());
    }

    /// <summary>
    ///     Net event: Apply delta changes from all clients
    /// </summary>
    private void OnWorldStateDeltaResponse(WorldStateDeltaList response, ISteamNetPeer peer)
    {
        foreach (var delta in response.DeltaList) {
            Delta.AddLocal(delta);
        }
    }

    /// <summary>
    ///     Net event: Check remote character's snapshot
    /// </summary>
    private void OnClientRemoteCharaSnapshot(CharaStateSnapshot response, ISteamNetPeer peer)
    {
        if (!States.TryGetValue(peer.Id, out var state)) {
            EmpLog.Warning("Received invalid remote character from player {@Peer}",
                peer);
            return;
        }

        if (response.State is null) {
            EmpLog.Warning("Received empty remote character state from player {@Peer}",
                peer);
            return;
        }

        state.LastAct = response.State.LastAct;
        state.Speed = response.State.Speed;
        state.LastReceivedTick = response.State.LastReceivedTick;

        NetSession.Instance.SharedSpeed = SharedSpeed;

        WorldStateSnapshot.CachedRemoteSnapshots.Add(response);

        var chara = ActiveRemoteCharas[peer.Id];
        response.ApplyReconciliation(chara);
    }

#region Scheduler Jobs

    /// <summary>
    ///     Subscribe all scheduler jobs and reset pause state
    ///     TODO profile the snapshot cost, see if we can use more granular hashed snapshot
    /// </summary>
    public void StartWorldStateUpdate()
    {
        // 5hz world snapshot reconciliation
        Scheduler.Subscribe(WorldStateSnapshotUpdate, 5);
        // 25hz delta dispatch
        Scheduler.Subscribe(WorldStateDeltaUpdate, 25);
        // 50hz delta process
        Scheduler.Subscribe(WorldStateDeltaProcess, 50);

        _pauseUpdate = false;
    }

    /// <summary>
    ///     Unsubscribe all scheduler jobs and reset pause state, also resets the server tick
    /// </summary>
    public void StopWorldStateUpdate()
    {
        Scheduler.Unsubscribe(WorldStateSnapshotUpdate);
        Scheduler.Unsubscribe(WorldStateDeltaUpdate);
        Scheduler.Unsubscribe(WorldStateDeltaProcess);

        NetSession.Instance.Tick = 0;

        _pauseUpdate = false;

        EmpLog.Debug("Stopping server state update");
    }

    /// <summary>
    ///     Pause sending out deltas, *but they still accumulate*
    /// </summary>
    public void PauseWorldStateUpdate()
    {
        _pauseUpdate = true;

        EmpLog.Debug("Pausing server state update");
    }

    /// <summary>
    ///     Resume sending out deltas
    /// </summary>
    public void ResumeWorldStateUpdate(bool clearDelta)
    {
        _pauseUpdate = false;

        EmpLog.Debug("Resuming server state update");

        if (clearDelta) {
            Delta.ClearOut();
        }
    }

#endregion
}