using System;
using System.Net;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using Exm.API;
using Exm.Helper;
using Exm.Model.Map;
using Steamworks;
using UnityEngine;
using YKF;

namespace Exm.Components;

public class MapCardView(IMapService service, MapMeta meta)
{
    public bool IsDirty { get; private set; } = true;

    private bool _expanded;

    public void Refresh()
    {
        IsDirty = false;
    }

    public void OnLayout(YKLayout layout)
    {
        var card = layout.MakeCard();

        var bannerGroup = card.Horizontal();
        BuildPreview(bannerGroup, bannerGroup.Layout);
        bannerGroup.Layout.spacing = 15f;

        var mapInfoGroup = bannerGroup.Vertical();
        mapInfoGroup.Layout.childAlignment = TextAnchor.UpperCenter;
        mapInfoGroup.Layout.spacing = 10f;

        var mapName = mapInfoGroup.Header(WebUtility.HtmlDecode(meta.Title));
        mapName.text1.fontSize *= 2;
        mapName.text1.alignment = TextAnchor.MiddleCenter;

        var mapStatGroup = mapInfoGroup.Horizontal();
        mapStatGroup.Layout.childForceExpandWidth = true;
        mapStatGroup.Layout.childControlHeight = true;
        mapStatGroup.Layout.spacing = 20f;

        BuildPrimaryStat(mapStatGroup);
        BuildSecondaryStat(bannerGroup);
        BuildControlButtons(bannerGroup);
    }

    public void Rate(int score, string? comment = null)
    {
        RateUpdateAsync().ForgetEx();

        return;

        async UniTask RateUpdateAsync()
        {
            var success = await service.UploadMapRatingAsync(meta.Id, new() {
                MapId = meta.Id,
                Author = SteamUser.GetSteamID().ToString(),
                Comment = comment,
                Score = score,
            });

            if (success) {
                IsDirty = true;
            }
        }
    }

    private void BuildPreview(YKLayout group, Component parent)
    {
        var bgSprite = "eg_no_preview".LoadSprite(resizeHeight: 128, resizeWidth: 128);
        var bg = group.AddImageCard(parent, bgSprite);
        var bgRt = bg.rectTransform;

        bgRt.pivot = new(0f, 0.5f);
        bgRt.anchorMin = new(0f, 0.5f);
        bgRt.anchorMax = new(0f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;

        bgRt.sizeDelta = new(110, 110);
        group.Spacer(0, 140);
    }

    private void BuildPrimaryStat(YKLayout group)
    {
        var primary = group.Vertical();
        primary.Layout.childAlignment = TextAnchor.UpperLeft;
        primary.Layout.spacing = 5f;

        var mapAuthor = primary.TextFlavor(WebUtility.HtmlDecode(meta.Author), FontColor.Myth);
        BuildRatingBar(primary);
    }

    private void BuildRatingBar(YKLayout group)
    {
        var barGroup = group.Horizontal();
        barGroup.Layout.childForceExpandWidth = true;

        var sprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48 static", "icons_48 static_4");
        for (var i = 0; i < 5; i++) {
            var filled = i < meta.RatingAverage;
            barGroup.AddImageCard(barGroup.Layout, sprite);
        }
        barGroup.Layout.spacing = 2f;
        barGroup.Layout.childAlignment = TextAnchor.MiddleLeft;
    }

    private void BuildSecondaryStat(YKLayout group)
    {
        var mapStatSecondary = group.Vertical();
        mapStatSecondary.Layout.childAlignment = TextAnchor.MiddleCenter;

        mapStatSecondary.Text($"Visits: {meta.VisitCount}")
            .alignment = TextAnchor.LowerRight;
        mapStatSecondary.Text($"Rating: {meta.RatingAverage:f1} ({meta.RatingCount})")
            .alignment = TextAnchor.LowerRight;

        var createTime = meta.Date;
        if (DateTime.TryParse(meta.Date, out var date)) {
            createTime = date.ToString("yyyy-MM-dd");
        }

        mapStatSecondary.TextFlavor(createTime)
            .alignment = TextAnchor.LowerRight;
        mapStatSecondary.TextFlavor(Version.Get(meta.Version).GetText())
            .alignment = TextAnchor.LowerRight;
    }

    private void BuildControlButtons(YKLayout group)
    {
        var controlGroup = group.Vertical();
        var controlGroupLe = controlGroup.LayoutElement();
        controlGroupLe.minWidth = 150f;
        controlGroupLe.preferredWidth = 150f;
        controlGroupLe.flexibleWidth = 0f;

        var detailGroup = group.transform.parent.GetComponent<YKLayout>().Vertical();

        detailGroup.SetActive(false);

        var enterBtn = controlGroup.Button("exm_ui_btn_enter".lang(), () => { });
        var expandBtn = controlGroup.Button("exm_ui_btn_expand".lang(), () => {
            detailGroup.SetActive(!_expanded);
            _expanded = !_expanded;
            Canvas.ForceUpdateCanvases();
            group.transform.parent.Rect().RebuildLayout(true);
        });
    }
}