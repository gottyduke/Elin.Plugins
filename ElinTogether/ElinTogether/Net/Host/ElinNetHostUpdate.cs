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

        Session.Tick++;
        Session.CurrentZone = _zone;

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
    internal void WorldStateDeltaUpdate()
    {
        if (_pauseUpdate) {
            return;
        }

        if (!Delta.HasPendingOut) {
            return;
        }

        if (Delta.FlushOutBuffer() is not { Count: > 0 } deltaList) {
            return;
        }

        Broadcast(new WorldStateDeltaList {
            DeltaList = deltaList,
        });
    }

    /// <summary>
    ///     Process local or deferred deltas received from clients
    /// </summary>
    internal void WorldStateDeltaProcess()
    {
        if (!Delta.HasPendingIn) {
            return;
        }

        Delta.ProcessLocalBatch(this);
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

        // if server disabled shared speed, we use -1
        Session.SharedSpeed = EmpConfig.Server.SharedAverageSpeed.Value
            ? SharedSpeed
            : -1;

        WorldStateSnapshot.CachedRemoteSnapshots.Add(response);

        var chara = ActiveRemoteCharas[peer.Id];
        response.ApplyReconciliation(chara);
    }

    private void UpdateRemotePlayerStates()
    {
        Broadcast(SessionPlayersSnapshot.Create());
    }

#region Scheduler Jobs

    /// <summary>
    ///     Subscribe all scheduler jobs and reset pause state
    ///     TODO profile the snapshot cost, see if we can use more granular hashed snapshot
    /// </summary>
    public void StartWorldStateUpdate()
    {
        // 0.5hz session player states update
        Scheduler.Subscribe(UpdateRemotePlayerStates, 0.5f);
        // 5hz world snapshot reconciliation
        Scheduler.Subscribe(WorldStateSnapshotUpdate, 5f);
        // // 25hz delta dispatch
        // Scheduler.Subscribe(WorldStateDeltaUpdate, 25f);
        // // 50hz delta process
        // Scheduler.Subscribe(WorldStateDeltaProcess, 50f);

        _pauseUpdate = false;
    }

    /// <summary>
    ///     Unsubscribe all scheduler jobs and reset pause state, also resets the server tick
    /// </summary>
    public void StopWorldStateUpdate()
    {
        Scheduler.Unsubscribe(UpdateRemotePlayerStates);
        Scheduler.Unsubscribe(WorldStateSnapshotUpdate);
        // Scheduler.Unsubscribe(WorldStateDeltaUpdate);
        // Scheduler.Unsubscribe(WorldStateDeltaProcess);

        Session.Tick = 0;

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