using System;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using EGate.API;
using EGate.Helper;
using EGate.Model.Map;
using Steamworks;
using UnityEngine;
using YKF;

namespace EGate.Components;

internal class MapCard(IMapService service, MapMeta meta)
{
    public bool IsDirty { get; private set; } = true;

    public void Refresh()
    {
        IsDirty = false;
    }

    public void OnLayout(YKLayout layout)
    {
        var card = layout.MakeCard();

        var bannerGroup = card.Horizontal();
        BuildPreview(bannerGroup, bannerGroup.Layout);

        var mapInfoGroup = bannerGroup.Vertical();
        mapInfoGroup.Layout.childAlignment = TextAnchor.UpperCenter;

        var mapName = mapInfoGroup.Header(meta.Title);
        mapName.text1.fontSize *= 2;

        var mapStatGroup = mapInfoGroup.Horizontal();
        mapStatGroup.Layout.childForceExpandWidth = true;
        mapStatGroup.Layout.childControlHeight = true;

        BuildPrimaryStat(mapStatGroup);
        BuildSecondaryStat(mapStatGroup);
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
        group.Spacer(0, (int)bgRt.sizeDelta.x + 25);
    }

    private void BuildPrimaryStat(YKLayout group)
    {
        var primary = group.Vertical();
        primary.Layout.childAlignment = TextAnchor.UpperLeft;
        primary.Layout.spacing = 5f;

        var mapAuthor = primary.TextFlavor(meta.Author, FontColor.Myth);
        BuildRatingBar(primary);
    }

    private void BuildRatingBar(YKLayout group)
    {
        var barGroup = group.Horizontal();
        barGroup.Layout.childForceExpandWidth = true;

        var sprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48 static", "icons_48 static_4");
        barGroup.AddImageCard(barGroup.Layout, sprite);
        barGroup.AddImageCard(barGroup.Layout, sprite);
        barGroup.AddImageCard(barGroup.Layout, sprite);
        barGroup.AddImageCard(barGroup.Layout, sprite);
        barGroup.AddImageCard(barGroup.Layout, sprite);
    }

    private void BuildSecondaryStat(YKLayout group)
    {
        var mapStatSecondary = group.Vertical();


        mapStatSecondary.TextFlavor($"Visits: {meta.VisitCount}");
        mapStatSecondary.TextFlavor($"Rating: {meta.RatingAverage:f1} ({meta.RatingCount})");

        var briefTime = meta.Date;
        if (DateTime.TryParse(meta.Date, out var date)) {
            briefTime = date.ToString("yyyy-MM-dd");
        }
        mapStatSecondary.Text(briefTime);
        mapStatSecondary.Text(Version.Get(meta.Version).GetText());
    }

    private void BuildControlButtons(YKLayout group)
    {
        var controlGroup = group.Vertical();
        var controlGroupLe = controlGroup.LayoutElement();
        controlGroupLe.minWidth = 150f;
        controlGroupLe.preferredWidth = 150f;
        controlGroupLe.flexibleWidth = 0f;

        var btn = controlGroup.Button("enter", () => { });
        var btn2 = controlGroup.Button("rate", () => { });
    }
}