using System;
using System.Collections.Generic;
using System.Net;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Exm.API;
using Exm.Helper;
using Exm.Model.Map;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Exm.Components;

public class MapCardView(IMapService service, MapMeta meta)
{
    private static Rect _refSize = UIHelper.FitWindow();
    private readonly List<UIItem> _ratingBar = [];
    private UIText? _author;
    private UIItem? _bg;
    private UIText? _date;
    private YKLayout? _detailGroup;

    private bool _expanded;
    // primary
    private UIItem? _name;
    // secondary
    private UIText? _rating;
    private UIText? _visits;

    public void OnLayout(YKLayout layout)
    {
        var card = layout.MakeCard();

        var bannerGroup = card.Horizontal();
        bannerGroup.Layout.childAlignment = TextAnchor.MiddleLeft;
        bannerGroup.Layout.childControlHeight = true;
        bannerGroup.Layout.spacing = 15f;
        bannerGroup.Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildPreview(bannerGroup, bannerGroup.Layout);
        BuildPrimaryStat(bannerGroup);
        BuildSecondaryStat(bannerGroup);
        BuildControlButtons(bannerGroup);
    }

    public void EnterMap()
    {
        var controller = ExmService.MapController;
        controller.LoadMap(meta);
    }

    private void BuildPreview(YKLayout group, Component parent)
    {
        // TODO api v2 load preview key
        var bgSprite = "exm_no_preview".LoadSprite(resizeHeight: 128, resizeWidth: 128);
        _bg = group.AddImageCard(parent, bgSprite)
            .WithMinWidth(128)
            .WithMinHeight(128)
            .WithWidth(128)
            .WithHeight(128);
        _bg.LayoutElement().flexibleWidth = 0f;
        _bg.image1.preserveAspect = false;
    }

    private void BuildPrimaryStat(YKLayout group)
    {
        var primary = group.Vertical();
        primary.Layout.childForceExpandWidth = false;
        primary.Layout.childAlignment = TextAnchor.MiddleLeft;
        primary.Layout.spacing = 5f;

        var le = primary.LayoutElement();
        le.flexibleWidth = 1f;
        le.minWidth = 0f;

        _name = primary.Header(WebUtility.HtmlDecode(meta.Title));
        _name.text1.fontSize *= 2;
        _name.text1.alignment = TextAnchor.MiddleLeft;

        _author = primary.TextFlavor(WebUtility.HtmlDecode(meta.Author), FontColor.Myth);

        BuildSubStatBar(primary);
        //BuildRatingBar(primary);
    }

    private void BuildSubStatBar(YKLayout group)
    {
        var subStat = group.Horizontal();
        subStat.Layout.childAlignment = TextAnchor.MiddleLeft;
        subStat.Fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        subStat.Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var visitSprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48", "icon_toGlobalMap");
        var visit = subStat.AddImageCard(subStat.Layout, visitSprite);

        var le = visit.LayoutElement();
        le.preferredHeight = 48f;
        le.preferredWidth = 48f;
        le.flexibleWidth = 0f;

        _visits = subStat.Text("exm_ui_visits".Loc(meta.VisitCount));
        _visits.alignment = TextAnchor.MiddleLeft;

        //subStat.Spacer(0, (int)(_refSize.width * 0.075f));

        var ratingSprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48 static", "icons_48 static_4");
        var rating = subStat.AddImageCard(subStat.Layout, ratingSprite);

        le = rating.LayoutElement();
        le.preferredHeight = 48f;
        le.preferredWidth = 48f;
        le.flexibleWidth = 0f;

        _rating = subStat.Text("exm_ui_likes".Loc(meta.RatingCount));
        _rating.alignment = TextAnchor.MiddleLeft;
    }

    private void BuildRatingBar(YKLayout group)
    {
        var barGroup = group.Horizontal();
        barGroup.Layout.childAlignment = TextAnchor.MiddleLeft;
        barGroup.Fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        barGroup.Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _ratingBar.Clear();

        var sprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48 static", "icons_48 static_4");
        for (var i = 0; i < 5; i++) {
            var rating = barGroup.AddImageCard(barGroup.Layout, sprite);

            var le = rating.LayoutElement();
            le.preferredHeight = 48f;
            le.preferredWidth = 48f;
            le.flexibleWidth = 0f;

            _ratingBar.Add(rating);
        }
    }

    private void BuildSecondaryStat(YKLayout group)
    {
        var statGroup = group.Vertical();
        statGroup.Layout.childAlignment = TextAnchor.MiddleCenter;
        statGroup.Layout.spacing = 2f;

        var createTime = meta.Date;
        if (DateTime.TryParse(meta.Date, out var date)) {
            createTime = date.ToString("yyyy-MM-dd");
        }

        _date = statGroup.TextFlavor(createTime);
        _date.alignment = TextAnchor.LowerRight;

        statGroup.TextFlavor(Version.Get(meta.Version).GetText())
            .alignment = TextAnchor.LowerRight;
    }

    private void BuildControlButtons(YKLayout group)
    {
        var controlGroup = group.Vertical();
        var le = controlGroup.LayoutElement();
        le.preferredWidth = _refSize.width * 0.2f;
        le.flexibleWidth = 0f;

        _detailGroup ??= group.transform.parent.GetComponent<YKLayout>().Vertical();

        _detailGroup.SetActive(false);

        var enterBtn = controlGroup.Button("exm_ui_btn_enter".lang(), EnterMap);
        var expandBtn = controlGroup.Button("exm_ui_btn_expand".lang(), () => {
            _detailGroup.SetActive(!_expanded);
            _expanded = !_expanded;
            Canvas.ForceUpdateCanvases();
            group.transform.parent.Rect().RebuildLayout(true);
        });
    }
}