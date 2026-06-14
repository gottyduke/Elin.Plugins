using System;
using System.Collections.Generic;
using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Net.Steam;
using Object = UnityEngine.Object;

namespace ElinTogether.Net;

public class NetSession : EClass
{
    public enum Mode : byte
    {
        None = 0,

        /// <summary>
        ///     player in the map alone, simulates <br />
        ///     TODO: full sync for now
        /// </summary>
        PartialSync,

        /// <summary>
        ///     players share the same map, first player simulates
        ///     host takes over whenever possible
        /// </summary>
        FullSync,
    }

    public static NetSession Instance => field ??= new();

    public Mode SyncMode { get; private set; } = Mode.None;
    public ElinNetBase? Connection { get; private set; }
    public Chara? Player { get; internal set; }
    public int SharedSpeed { get; internal set; }
    public Zone? CurrentZone { get; internal set; }
    public int Tick { get; internal set; }
    public ulong SessionId { get; internal set; }
    public NetSessionRules Rules { get; internal set; } = NetSessionRules.Default;
    public bool IsRejoining { get; internal set; }

    public List<NetPeerState> CurrentPlayers => field ??= [];
    public SteamNetLobbyManager Lobby => field ??= new();

    public bool HasActiveConnection => Connection != null && Connection.IsConnected;
    public bool IsHost => Connection?.IsHost is not false;
    public bool IsClient => !IsHost;
    public bool ShouldSimulate => IsHost || SyncMode == Mode.PartialSync;

    /// <summary>
    ///     Minimal info retained across transient disconnects to support lightweight rejoin.
    ///     Populated on successful initial join / synchronization.
    /// </summary>
    public LastSessionInfo? LastSession { get; internal set; }

    // ===== Connection Phase State Machine (per design doc) =====

    public ConnectionPhase CurrentPhase { get; private set; } = ConnectionPhase.None;

    public event Action<ConnectionPhase>? OnPhaseChanged;

    /// <summary>
    ///     Transitions the session to a new connection phase. Logs the change and fires
    ///     <see cref="OnPhaseChanged" />. Idempotent if already in target phase.
    /// </summary>
    public void SetPhase(ConnectionPhase phase)
    {
        if (CurrentPhase == phase) {
            return;
        }

        var previous = CurrentPhase;
        CurrentPhase = phase;

        EmpLog.Debug("ConnectionPhase: {Previous} -> {Current}", previous, phase);
        OnPhaseChanged?.Invoke(phase);
    }

    /// <summary>
    ///     Resets phase to None (used on RemoveComponent / full disconnect).
    /// </summary>
    public void ResetPhase()
    {
        CurrentPhase = ConnectionPhase.None;
    }

    public void RemoveComponent()
    {
        if (Connection != null) {
            if (!Connection.IsHost && core.IsGameStarted) {
                ui.RemoveLayers();
                game.Kill();
            }

            Object.DestroyImmediate(Connection);

            EmpLog.Debug("Removed connection component of {NetType}",
                Connection.GetType().Name);
        }

        Connection = null;
        Tick = 0;
        CurrentPlayers.Clear();

        ResourceFetch.InvalidateTemp();

        SwitchSyncMode(Mode.None);
        ResetPhase();
    }

    public T InitializeComponent<T>() where T : ElinNetBase
    {
        RemoveComponent();

        Connection = EmpMod.Instance.gameObject.AddComponent<T>();

        EmpLog.Debug("Initialized new connection component of {NetType}",
            typeof(T).Name);

        return (Connection as T)!;
    }

    public void SwitchSyncMode(Mode mode)
    {
        if (SyncMode == mode) {
            return;
        }

        EmpLog.Debug("Switched SyncMode to {SyncMode}", mode);

        SyncMode = mode;
    }
}