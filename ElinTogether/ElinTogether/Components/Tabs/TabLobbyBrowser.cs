using ElinTogether.Net;
using ElinTogether.Net.Steam;

namespace ElinTogether.Components;

internal class TabLobbyBrowser : TabEmpBase
{
    public override void OnLayout()
    {
        BuildNetButtons();
        BuildLobbyList();
    }

    private void BuildNetButtons()
    {
        var btnGroup = Horizontal();
        btnGroup.Layout.childForceExpandWidth = true;

        if (!EClass.core.IsGameStarted) {
            btnGroup.Header("Server can only be started in an active save");
            return;
        }

        if (NetSession.Instance.Connection == null) {
            btnGroup.Button("Start Server", StartServerFromPanel);
        } else {
            btnGroup.Button("Invite Friend", SteamNetLobbyManager.Instance.InviteSteamOverlay);
            btnGroup.Button("Disconnect", DisconnectFromPanel);
        }
    }

    private void BuildLobbyList()
    {
        Spacer(5);

        var totalPlayers = Header("Total Online Players : ");

        SteamNetLobbyManager.Instance.GetOnlineLobbies(SetupLobbyDisplay);

        return;

        void SetupLobbyDisplay(SteamNetLobby[] lobbies)
        {
            var total = 0;

            foreach (var lobby in lobbies) {
                var count = lobby.GetCurrentPlayersCount();
                total += count;

                HeaderCard($"{lobby.OwnerName}'s Game [{lobby.GameVersion}] with " +
                           $"{lobby.PlayerCount} Player, at {lobby.CurrentZone}");
            }

            totalPlayers.text1.text += total;
        }
    }

    private void StartServerFromPanel()
    {
        NetSession.Instance.InitializeComponent<ElinNetHost>().StartServer();
        LayerElinTogether.Instance?.Reopen();
    }

    private void DisconnectFromPanel()
    {
        var isClient = NetSession.Instance.Connection is ElinNetClient;

        NetSession.Instance.RemoveComponent();
        LayerElinTogether.Instance?.Reopen();

        if (isClient) {
            EMono.scene.Init(Scene.Mode.Title);
        }
    }
}