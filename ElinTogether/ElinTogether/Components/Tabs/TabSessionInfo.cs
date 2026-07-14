using ElinTogether.Helper;
using ElinTogether.Helper.String;
using ElinTogether.LangMod;
using ElinTogether.Net;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace ElinTogether.Components;

internal class TabSessionInfo : TabEmpBase
{
    private Rect _refSize = LayerElinTogether.Instance!.Bound;

    private void Update()
    {

    }

    public override void OnLayout()
    {
        BuildOverviewSection();
        BuildPlayerList();
    }

    private void BuildOverviewSection()
    {
    }

    private void BuildPlayerList()
    {
        Header("emp_ui_connected_players");

        var list = Grid()
            .WithConstraintCount(2);
        list.Fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        list.Layout.cellSize = new(_refSize.width / 2.2f, _refSize.width * 0.2f);
        var players = NetSession.Instance.CurrentPlayers;
        foreach (var player in players) {
            BuildPlayerCard(list, player);
        }
    }

    private void BuildPlayerCard(YKLayout parent, NetPeerState player)
    {
        var chara = player.FindChara();
        if (chara is null) {
            Text("emp_ui_invalid_chara".Loc(player.Index, player.CharaUid));
            return;
        }

        var card = parent.MakeCard();
        var bannerGroup = card.Horizontal();
        bannerGroup.Layout.spacing = 15f;

        // portrait
        var go = new GameObject("portrait");
        go.transform.SetParent(bannerGroup.transform);
        var portrait = go.AddComponent<Portrait>();
        portrait.tooltip = new() {
            enable = false,
        };
        portrait.portrait = go.AddComponent<Image>();
        portrait.portrait.preserveAspect = true;

        var go1 = new GameObject("overlay");
        go1.transform.SetParent(go.transform);
        portrait.overlay = go1.AddComponent<Image>();
        portrait.overlay.preserveAspect = true;

        portrait.SetChara(chara);

        portrait.portrait.LayoutElement().preferredWidth = _refSize.width * 0.12f;
        portrait.portrait.rectTransform.sizeDelta = new(_refSize.width, _refSize.width * 0.16f);
        portrait.overlay.rectTransform.anchorMin =
            portrait.overlay.rectTransform.anchorMax =
                portrait.overlay.rectTransform.sizeDelta = Vector2.zero;

        // info
        var infoGroup = bannerGroup.Vertical();
        infoGroup.LayoutElement().preferredWidth = 1f;
        infoGroup.TextFlavor(player.Name.TagColor(PeerColorizer.GetColor(player.Index)));
        infoGroup.TextMedium(chara.Name);
        infoGroup.Text(BuildPingStat(player));
    }

    private static string BuildPingStat(NetPeerState player)
    {
        if (player.Index == 0) {
            return "Ping: N/A (Host)";
        }

        var ping = player.AvgPingMs > 0 ? player.AvgPingMs : player.LastPingMs;
        var quality = player.ConnectionQualityLocal > 0
            ? $" | Quality: {player.ConnectionQualityLocal:P0}"
            : "";

        return $"Ping: {ping:F0}ms{quality}";
    }
}