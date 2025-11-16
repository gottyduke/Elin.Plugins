using System;
using System.Collections.Generic;
using Cwl.Helper.Unity;
using ElinTogether.Helper;
using Steamworks;

namespace ElinTogether.Net.Steam;

public class SteamNetLobbyManager
{
    private Action<SteamNetLobby[]>? _deferOnComplete;

    private SteamNetLobbyManager()
    {
        SteamCallback<LobbyCreated_t>.Add(OnLobbyCreated);
        SteamCallback<LobbyChatUpdate_t>.Add(OnLobbyChatUpdate);
        SteamCallback<GameLobbyJoinRequested_t>.Add(OnLobbyJoinRequested);
        SteamCallback<LobbyEnter_t>.Add(OnLobbyEntered);
        SteamCallback<LobbyMatchList_t>.Add(OnLobbyMatchListComplete);
    }

    public static SteamNetLobbyManager Instance => field ??= new();

    public SteamNetLobby? CurrentLobby { get; private set; }

    /// <summary>
    ///     Create a new lobby. We do this automatically on Host
    /// </summary>
    public void CreateLobby(SteamNetLobbyType type = SteamNetLobbyType.Invite, int maxPlayers = 16)
    {
        LeaveLobby();

        EmpLog.Information("Creating steam {LobbyType} lobby",
            type);

        var lobbyType = type switch {
            SteamNetLobbyType.Public => ELobbyType.k_ELobbyTypePublic,
            SteamNetLobbyType.Friend => ELobbyType.k_ELobbyTypeFriendsOnly,
            // we use public to be able to search in list
            // though we do not join from here
            SteamNetLobbyType.Invite => ELobbyType.k_ELobbyTypePublic,
            _ => throw new ArgumentOutOfRangeException(nameof(SteamNetLobbyType), type, null),
        };

        SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
    }

    /// <summary>
    ///     Leave current lobby if it's valid
    /// </summary>
    public void LeaveLobby()
    {
        if (CurrentLobby is not null) {
            SteamMatchmaking.LeaveLobby(CurrentLobby.LobbyId);
            EmpLog.Information("Left steam lobby");
        }

        CurrentLobby = null;
    }

    /// <summary>
    ///     Connect by lobby id
    /// </summary>
    public void ConnectLobby(ulong lobbyId)
    {
        LeaveLobby();

        if (EClass.core.IsGameStarted) {
            EClass.game.Kill();
            EMono.scene.Init(Scene.Mode.Title);
        }

        ELayerCleanup.Cleanup<LayerHelp>();

        SteamMatchmaking.JoinLobby((CSteamID)lobbyId);
    }

    /// <summary>
    ///     Invite by steam user id
    /// </summary>
    public void InviteSteamUser(ulong steamId64)
    {
        if (CurrentLobby is not null) {
            SteamMatchmaking.InviteUserToLobby(CurrentLobby.LobbyId, (CSteamID)steamId64);
        }
    }

    /// <summary>
    ///     Invite by opening up overlay, requires launching from steam
    /// </summary>
    public void InviteSteamOverlay()
    {
        if (CurrentLobby is not null) {
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobby.LobbyId);
        }
    }

    /// <summary>
    ///     Fetch all current online lobbies
    /// </summary>
    public void GetOnlineLobbies(Action<SteamNetLobby[]> onComplete)
    {
        _deferOnComplete = onComplete;
        //SteamMatchmaking.AddRequestLobbyListStringFilter("EmpVersion", ModInfo.BuildVersion, 0);
        SteamMatchmaking.RequestLobbyList();
    }

    /// <summary>
    ///     Parse from steam launch args
    /// </summary>
    internal void TryParseLobbyCommand()
    {
        ulong lobbyId = 0;
        var args = Environment.GetCommandLineArgs();

        for (var i = 0; i < args.Length; i++) {
            if (!string.Equals(args[i], "+connect_lobby", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (i + 1 < args.Length && ulong.TryParse(args[i + 1], out lobbyId)) {
                break;
            }
        }

        if (lobbyId != 0) {
            ConnectLobby(lobbyId);
        }
    }

#region Steam Callbacks

    private void OnLobbyCreated(LobbyCreated_t lobby)
    {
        EmpPop.Information("Steam lobby created");

        CurrentLobby = new((CSteamID)lobby.m_ulSteamIDLobby);

        CurrentLobby.SetLobbyData("EmpVersion", ModInfo.BuildVersion);

        // add our custom lobby data
        CurrentLobby.SetLobbyData("OwnerName", SteamFriends.GetPersonaName());
        CurrentLobby.SetLobbyData("GameVersion", EMono.core.version.GetText());

        NetSession.Instance.SessionId = lobby.m_ulSteamIDLobby;
    }

    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t request)
    {
        var lobbyId = request.m_steamIDLobby;

        EmpPop.Information("Received lobby join request {LobbyId}",
            lobbyId);

        ConnectLobby((ulong)lobbyId);
    }

    private void OnLobbyEntered(LobbyEnter_t state)
    {
        CurrentLobby = new((CSteamID)state.m_ulSteamIDLobby);
        CurrentLobby.RefreshData();

        NetSession.Instance.SessionId = state.m_ulSteamIDLobby;

        var host = CurrentLobby.GetLobbyOwner();
        // connect to host automatically
        if (host == SteamUser.GetSteamID()) {
            return;
        }

        EmpPop.Information("Joined lobby, current EmpVersion is {EmpVersion}",
            CurrentLobby.GetLobbyData("EmpVersion"));

        NetSession.Instance
            .InitializeComponent<ElinNetClient>()
            .ConnectSteamUser((ulong)host);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t update)
    {
        var member = (CSteamID)update.m_ulSteamIDUserChanged;
        var state = (SteamNetLobbyMemberState)update.m_rgfChatMemberStateChange;

        SteamUserName.PinUserName(update.m_ulSteamIDUserChanged, name => EmpPop.Information(
            "Player lobby state changed\n{@LobbyState}",
            new {
                Name = name,
                State = state,
            }));

        if (member != CurrentLobby!.GetLobbyOwner()) {
            return;
        }

        // we also leave lobby if host is gone
        NetSession.Instance.RemoveComponent();
        LeaveLobby();
    }

    private void OnLobbyMatchListComplete(LobbyMatchList_t list)
    {
        var fetched = list.m_nLobbiesMatching;
        List<SteamNetLobby> lobbies = [];

        for (var i = 0; i < fetched; ++i) {
            var lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            if (lobbyId == CSteamID.Nil) {
                continue;
            }

            var lobby = new SteamNetLobby(lobbyId);
            lobby.RefreshData();

            lobbies.Add(lobby);
        }

        _deferOnComplete?.Invoke(lobbies.ToArray());
        _deferOnComplete = null;
    }

#endregion
}