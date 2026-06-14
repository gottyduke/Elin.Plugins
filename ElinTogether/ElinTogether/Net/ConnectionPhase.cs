namespace ElinTogether.Net;

public enum ConnectionPhase
{
    /// <summary>No active multiplayer session.</summary>
    None = 0,

    /// <summary>Host is creating the Steam lobby (async).</summary>
    LobbyCreating,

    /// <summary>Lobby created, starting P2P listen socket.</summary>
    HostingListening,

    /// <summary>Host is fully ready, accepting connections, self-registered.</summary>
    HostingReady,

    /// <summary>Client is joining a Steam lobby (async, via invite or browser).</summary>
    LobbyJoining,

    /// <summary>Client has lobby context, initiating P2P connection to host.</summary>
    Connecting,

    /// <summary>Transport connected, performing version/auth checks.</summary>
    Authenticating,

    /// <summary>Exchanging and validating source data.</summary>
    SourceValidating,

    /// <summary>这个source它怎么就不同步呢.</summary>
    SourceSynced,

    /// <summary>Determining/creating remote character (new embark or reuse saved).</summary>
    PlayerPreparing,

    /// <summary>Receiving full save probe (initial join) or rejoin data.</summary>
    ReceivingWorldState,

    /// <summary>Loading zone data and activating position.</summary>
    WorldLoading,

    /// <summary>Fully synchronized, deltas and snapshots flowing. Normal play state.</summary>
    Synchronized,

    /// <summary>Attempting to resume a previous session after disconnect.</summary>
    Reconnecting,

    /// <summary>Graceful or forced disconnect in progress.</summary>
    Disconnecting,

    /// <summary>Session ended (can auto reconnect if lobby is available).</summary>
    Disconnected,
}

public static class ConnectionPhaseExtensions
{
    extension(ConnectionPhase phase)
    {
        public bool IsHostPhase =>
            phase is ConnectionPhase.LobbyCreating or ConnectionPhase.HostingListening or ConnectionPhase.HostingReady;

        public bool IsClientJoinPhase =>
            phase is ConnectionPhase.LobbyJoining or ConnectionPhase.Connecting or ConnectionPhase.Authenticating;

        public bool IsActivePlay => phase is ConnectionPhase.Synchronized;
        public bool CanAttemptReconnect => phase is ConnectionPhase.Disconnected or ConnectionPhase.Synchronized;

        public string DisplayLangKey => phase switch {
            ConnectionPhase.None => "emp_phase_none",
            ConnectionPhase.LobbyCreating => "emp_phase_lobby_creating",
            ConnectionPhase.HostingListening => "emp_phase_host_listening",
            ConnectionPhase.HostingReady => "emp_phase_host_ready",
            ConnectionPhase.LobbyJoining => "emp_phase_lobby_joining",
            ConnectionPhase.Connecting => "emp_phase_connecting",
            ConnectionPhase.Authenticating => "emp_phase_auth",
            ConnectionPhase.SourceValidating => "emp_phase_source_validating",
            ConnectionPhase.SourceSynced => "emp_phase_source_synced",
            ConnectionPhase.PlayerPreparing => "emp_phase_player_prep",
            ConnectionPhase.ReceivingWorldState => "emp_phase_receiving_world",
            ConnectionPhase.WorldLoading => "emp_phase_world_loading",
            ConnectionPhase.Synchronized => "emp_phase_synchronized",
            ConnectionPhase.Reconnecting => "emp_phase_reconnecting",
            ConnectionPhase.Disconnecting => "emp_phase_disconnecting",
            ConnectionPhase.Disconnected => "emp_phase_disconnected",
            _ => "emp_phase_unknown",
        };
    }
}