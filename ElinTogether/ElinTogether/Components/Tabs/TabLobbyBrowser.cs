using Cwl.LangMod;
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
            btnGroup.Header("emp_ui_unclaimed_zone");
            return;
        }

        if (NetSession.Instance.Connection == null) {
            btnGroup.Button("emp_ui_sv_start".lang(), StartServerFromPanel);
        } else {
            btnGroup.Button("emp_ui_sv_invite".lang(), NetSession.Instance.Lobby.InviteSteamOverlay);
            btnGroup.Button("emp_ui_sv_dc".lang(), DisconnectFromPanel);
        }
    }

    private void BuildLobbyList()
    {
        Spacer(5);

        var totalPlayers = Header("");

        NetSession.Instance.Lobby.GetOnlineLobbies(SetupLobbyDisplay);

        return;

        void SetupLobbyDisplay(SteamNetLobby[] lobbies)
        {
            var total = 0;

            foreach (var lobby in lobbies) {
                var count = lobby.GetCurrentPlayersCount();
                total += count;

                HeaderCard("emp_ui_lobby_desc".Loc(lobby.OwnerName, lobby.GameVersion, lobby.PlayerCount, lobby.CurrentZone));
            }

            totalPlayers.text1.text = "emp_ui_lobby_tally".Loc(total);
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